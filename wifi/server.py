"""A local, simulated, read-only counter. Standard library only."""

import argparse
import json
import math
import time
from functools import partial
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import urlsplit


class Counter:
    def __init__(self, freeze_after=None, clock=time.monotonic):
        self.clock = clock
        self.started = clock()
        self.freeze_after = freeze_after

    def snapshot(self):
        elapsed = max(0, self.clock() - self.started)
        if self.freeze_after is not None:
            elapsed = min(elapsed, self.freeze_after)
        return {"version": 1, "simulated": True, "counter": int(elapsed)}


class Handler(BaseHTTPRequestHandler):
    def __init__(self, *args, counter, **kwargs):
        self.counter = counter
        super().__init__(*args, **kwargs)

    def do_GET(self):
        path = urlsplit(self.path).path
        if path == "/":
            body = Path(__file__).with_name("index.html").read_bytes()
            content_type = "text/html; charset=utf-8"
        elif path == "/api/counter":
            body = json.dumps(self.counter.snapshot()).encode("utf-8")
            content_type = "application/json"
        else:
            self.send_error(404)
            return
        self.send_response(200)
        self.send_header("Content-Type", content_type)
        self.send_header("Content-Length", str(len(body)))
        self.send_header("Cache-Control", "no-store")
        self.send_header("X-Content-Type-Options", "nosniff")
        self.end_headers()
        self.wfile.write(body)

    def reject_write(self):
        self.send_error(405, "Read-only proof; writes are not supported")

    do_POST = reject_write
    do_PUT = reject_write
    do_PATCH = reject_write
    do_DELETE = reject_write


def create_server(host, port, counter=None):
    return ThreadingHTTPServer(
        (host, port), partial(Handler, counter=counter if counter is not None else Counter())
    )


def nonnegative_seconds(value):
    number = float(value)
    if not math.isfinite(number) or number < 0:
        raise argparse.ArgumentTypeError("must be finite and nonnegative")
    return number


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--host", default="127.0.0.1", help="local IPv4 address to bind")
    parser.add_argument("--port", type=int, default=8000)
    parser.add_argument("--freeze-after", type=nonnegative_seconds)
    args = parser.parse_args()
    if not 1 <= args.port <= 65535:
        parser.error("--port must be between 1 and 65535")
    with create_server(args.host, args.port, Counter(args.freeze_after)) as server:
        print(f"SIMULATED only: http://{args.host}:{args.port}/", flush=True)
        print("Ctrl+C to stop. No network settings have been changed.", flush=True)
        try:
            server.serve_forever()
        except KeyboardInterrupt:
            print("\nStopped.")


if __name__ == "__main__":
    main()
