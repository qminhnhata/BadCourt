#!/usr/bin/env python3
"""Build a documentation PDF from its markdown source.

Pipeline:  markdown --(npx marked)--> HTML fragment --(this script)--> styled HTML
           --(headless Chrome)--> PDF

Usage:  python build-pdf.py [document]

        document defaults to 'implementation-plan'; pass 'phase-0-plan'
        (with or without the .md suffix) to build the Phase 0 plan.
"""
import os
import subprocess
import sys
import shutil
from pathlib import Path

HERE = Path(__file__).resolve().parent
BUILD = HERE / "build"

# One entry per document. 'heading' and 'meta' become the styled title block that
# replaces the markdown-rendered <h1>; 'title' is the HTML <title>. 'section_breaks'
# starts every h2 on a fresh page - right for long sections, wasteful for short ones.
DOCS = {
    "implementation-plan": {
        "section_breaks": True,
        "strip_lead": True,
        "title": "BadCourt - Rewrite Implementation Plan",
        "heading": "BadCourt &mdash; Rewrite Implementation Plan",
        "meta": (
            "<strong>Project:</strong> BadCourt &mdash; Badminton Court Booking Platform<br>"
            "<strong>Supersedes:</strong> <code>se121.badcourt</code> "
            "(.NET 9 microservices + Flutter + Next.js)<br>"
            "<strong>Date:</strong> 2 September 2026"
        ),
    },
    "phase-0-plan": {
        "section_breaks": False,
        "strip_lead": False,
        "title": "BadCourt - Phase 0: Foundation",
        "heading": "BadCourt &mdash; Phase 0: Foundation",
        "meta": (
            "<strong>Phase:</strong> 0 of 10 &mdash; Foundation<br>"
            "<strong>Shape:</strong> 21 steps across 5 stages, one commit per step<br>"
            "<strong>Exit criteria:</strong> <code>dotnet test</code> green; "
            "Aspire dashboard shows a healthy app<br>"
            "<strong>Date:</strong> 2 September 2026"
        ),
    },
}
DEFAULT_DOC = "implementation-plan"

CHROME_CANDIDATES = [
    r"C:\Program Files\Google\Chrome\Application\chrome.exe",
    r"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
    r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
    r"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
]

CSS = """
@page { size: A4; margin: 18mm 16mm 18mm 16mm; }

* { box-sizing: border-box; }

html { -webkit-print-color-adjust: exact; print-color-adjust: exact; }

body {
  font-family: "Segoe UI", -apple-system, "Helvetica Neue", Arial, sans-serif;
  font-size: 10.2pt;
  line-height: 1.55;
  color: #1a1d21;
  margin: 0;
}

/* ---------- title block ---------- */
.title-block {
  border-bottom: 3px solid #0f766e;
  padding-bottom: 14px;
  margin-bottom: 26px;
}
.title-block h1 {
  border: 0; margin: 0 0 6px 0; padding: 0;
  font-size: 24pt; letter-spacing: -0.4px; color: #0f172a;
}
.title-block .meta { font-size: 9.5pt; color: #52606d; line-height: 1.7; }
.title-block .meta strong { color: #1a1d21; font-weight: 600; }

/* ---------- headings ---------- */
h1, h2, h3, h4 {
  color: #0f172a; font-weight: 650;
  line-height: 1.25;
  break-after: avoid-page; page-break-after: avoid;
  margin: 1.6em 0 0.55em;
}
h1 { font-size: 19pt; }
h2 {
  font-size: 15pt;
  border-bottom: 1.5px solid #d7dee5;
  padding-bottom: 5px;
  margin-top: 1.9em;
  break-before: page; page-break-before: always;
}
h2:first-of-type { break-before: auto; page-break-before: auto; }
h3 { font-size: 12pt; color: #0f766e; }
h4 { font-size: 10.5pt; }

p { margin: 0.7em 0; orphans: 3; widows: 3; }

/* ---------- lists ---------- */
ul, ol { margin: 0.7em 0; padding-left: 1.5em; }
li { margin: 0.32em 0; }
li > strong:first-child { color: #0f172a; }

/* ---------- code ---------- */
code {
  font-family: "Cascadia Mono", Consolas, "SF Mono", "Courier New", monospace;
  font-size: 0.87em;
  background: #eef2f5;
  color: #0b3f3a;
  padding: 1px 4px;
  border-radius: 3px;
  white-space: nowrap;
}
pre {
  background: #f7f9fb;
  border: 1px solid #dde5ec;
  border-left: 3px solid #0f766e;
  border-radius: 4px;
  padding: 11px 13px;
  margin: 1em 0;
  overflow: visible;
  break-inside: avoid-page; page-break-inside: avoid;
}
pre code {
  background: none; color: #16302e; padding: 0;
  font-size: 7.6pt; line-height: 1.42;
  white-space: pre-wrap; word-break: break-word;
}

/* ---------- tables ---------- */
table {
  border-collapse: collapse;
  width: 100%;
  margin: 1.1em 0;
  font-size: 8.6pt;
  break-inside: auto; page-break-inside: auto;
}
thead { display: table-header-group; }
tr { break-inside: avoid; page-break-inside: avoid; }
th {
  background: #0f766e; color: #fff;
  text-align: left; font-weight: 600;
  padding: 7px 9px;
  border: 1px solid #0f766e;
  line-height: 1.35;
}
td {
  padding: 6px 9px;
  border: 1px solid #dbe2e8;
  vertical-align: top;
  line-height: 1.45;
}
tbody tr:nth-child(even) { background: #f6f9fa; }
td code, th code { font-size: 0.9em; white-space: normal; }

/* ---------- blockquote ---------- */
blockquote {
  margin: 1.1em 0;
  padding: 10px 15px;
  background: #fdf7e8;
  border-left: 3px solid #d99a2b;
  color: #4a3d21;
  break-inside: avoid-page; page-break-inside: avoid;
}
blockquote p { margin: 0.35em 0; }

/* ---------- rules ---------- */
hr { border: 0; border-top: 1px solid #e2e8ee; margin: 1.8em 0; }

/* the --- separators sit right before each h2, which already breaks */
hr + h2 { margin-top: 0; }

em { color: #52606d; }
"""


def find_chrome() -> str:
    for path in CHROME_CANDIDATES:
        if os.path.isfile(path):
            return path
    for name in ("chrome", "chromium", "msedge"):
        found = shutil.which(name)
        if found:
            return found
    sys.exit("ERROR: no Chrome/Edge binary found for PDF rendering.")


def resolve_doc(argv: list[str]) -> str:
    name = argv[1] if len(argv) > 1 else DEFAULT_DOC
    if name.endswith(".md"):
        name = name[:-3]
    if name not in DOCS:
        known = ", ".join(sorted(DOCS))
        sys.exit(f"ERROR: unknown document {name!r}. Known documents: {known}.")
    return name


def main() -> None:
    name = resolve_doc(sys.argv)
    doc = DOCS[name]
    src = HERE / f"{name}.md"
    body_file = BUILD / f"{name}.body.html"
    page = BUILD / f"{name}.html"
    pdf = HERE / f"{name}.pdf"

    if not src.is_file():
        sys.exit(f"ERROR: {src} not found.")
    BUILD.mkdir(exist_ok=True)

    # 1. markdown -> html fragment
    npx = shutil.which("npx") or shutil.which("npx.cmd")
    if not npx:
        sys.exit("ERROR: npx not found on PATH.")
    subprocess.run(
        [npx, "--yes", "marked", "--gfm", "-i", str(src), "-o", str(body_file)],
        check=True, shell=(os.name == "nt"),
    )

    body = body_file.read_text(encoding="utf-8")

    # 2. lift the leading title + meta paragraph into a styled title block
    heading = doc["heading"]
    meta = doc["meta"]
    title_block = (
        '<div class="title-block">'
        f"<h1>{heading}</h1>"
        f'<div class="meta">{meta}</div>'
        "</div>"
    )
    # drop the markdown-rendered h1. 'strip_lead' additionally drops the paragraph
    # after it, for documents whose h1 is followed by a meta line rather than prose;
    # guessing by position instead would eat the first real section of a document
    # that opens straight into a heading.
    start = body.find("<h1")
    if start != -1:
        after_h1 = body.find("</h1>", start)
        if after_h1 != -1:
            rest = body[after_h1 + 5:]
            if doc.get("strip_lead", True):
                p_start = rest.find("<p>")
                p_end = rest.find("</p>")
                if p_start != -1 and p_end != -1 and p_start < 40:
                    rest = rest[p_end + 4:]
            # also drop the immediately following <hr>
            rest = rest.lstrip()
            if rest.startswith("<hr>"):
                rest = rest[4:]
            body = rest

    # 3. let short documents flow instead of paginating on every section
    css = CSS
    if not doc.get("section_breaks", True):
        css += (
            "\n/* short document: sections flow, the hr carries the separation */\n"
            "h2 { break-before: auto; page-break-before: auto; }\n"
            "table, pre { break-inside: avoid-page; page-break-inside: avoid; }\n"
        )

    html = (
        "<!DOCTYPE html>\n<html lang=\"en\">\n<head>\n"
        "<meta charset=\"utf-8\">\n"
        f"<title>{doc['title']}</title>\n"
        f"<style>{css}</style>\n</head>\n<body>\n"
        f"{title_block}\n{body}\n</body>\n</html>\n"
    )
    page.write_text(html, encoding="utf-8")

    # 4. html -> pdf
    chrome = find_chrome()
    url = page.as_uri()
    subprocess.run(
        [
            chrome,
            "--headless",
            "--disable-gpu",
            "--no-sandbox",
            "--no-pdf-header-footer",
            "--run-all-compositor-stages-before-draw",
            "--virtual-time-budget=10000",
            f"--print-to-pdf={pdf}",
            url,
        ],
        check=True, capture_output=True,
    )

    if not pdf.is_file():
        sys.exit("ERROR: Chrome did not produce a PDF.")
    print(f"OK  {pdf}  ({pdf.stat().st_size:,} bytes)")


if __name__ == "__main__":
    main()
