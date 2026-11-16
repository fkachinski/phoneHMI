import argparse
import json
import threading
import unittest
from urllib.error import HTTPError
from urllib.request import ProxyHandler, Request, build_opener

from server import Counter, create_server, nonnegative_seconds


class CounterTests(unittest.TestCase):
    def test_counter_advances_and_freezes(self):
        now = [100.0]
        counter = Counter(freeze_after=3, clock=lambda: now[0])
        self.assertEqual(counter.snapshot(), {"version": 1, "simulated": True, "counter": 0})
        now[0] = 102.5
        self.assertEqual(counter.snapshot()["counter"], 2)
        now[0] = 110
        self.assertEqual(counter.snapshot()["counter"], 3)
        now[0] = 120
        self.assertEqual(counter.snapshot()["counter"], 3)

    def test_unfrozen_counter_keeps_advancing(self):
        now = [0]
        counter = Counter(clock=lambda: now[0])
        now[0] = 60
        self.assertEqual(counter.snapshot()["counter"], 60)

    def test_freeze_time_rejects_nonfinite_and_negative_values(self):
        for value in ("nan", "inf", "-1"):
            with self.subTest(value=value), self.assertRaises(argparse.ArgumentTypeError):
                nonnegative_seconds(value)


class HttpTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.server = create_server("127.0.0.1", 0, Counter(freeze_after=0))
        cls.thread = threading.Thread(target=cls.server.serve_forever)
        cls.thread.start()
        cls.base = f"http://127.0.0.1:{cls.server.server_port}"
        cls.client = build_opener(ProxyHandler({}))

    @classmethod
    def tearDownClass(cls):
        cls.server.shutdown()
        cls.thread.join()
        cls.server.server_close()

    def test_page_and_uncached_counter(self):
        with self.client.open(self.base + "/", timeout=3) as response:
            self.assertIn(b"SIMULATED", response.read())
        with self.client.open(self.base + "/api/counter", timeout=3) as response:
            self.assertEqual(response.headers["Cache-Control"], "no-store")
            self.assertEqual(json.load(response), {"version": 1, "simulated": True, "counter": 0})

    def test_unknown_paths_do_not_serve_files(self):
        for path in ("/missing", "/server.py", "/../README.md"):
            with self.subTest(path=path), self.assertRaises(HTTPError) as error:
                self.client.open(self.base + path, timeout=3)
            self.assertEqual(error.exception.code, 404)
            error.exception.close()

    def test_write_methods_are_rejected(self):
        for method in ("POST", "PUT", "PATCH", "DELETE"):
            with self.subTest(method=method), self.assertRaises(HTTPError) as error:
                self.client.open(Request(self.base + "/api/counter", method=method), timeout=3)
            self.assertEqual(error.exception.code, 405)
            error.exception.close()


if __name__ == "__main__":
    unittest.main()
