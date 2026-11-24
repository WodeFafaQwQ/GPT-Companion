"""Normalize downloaded dictionary sources into the offline vocabulary resource."""
import csv
import gzip
import json
import io
import zipfile
from pathlib import Path

project = Path(__file__).resolve().parent.parent
source = project / "tests" / "vocabulary-source"
destination = project / "assets" / "vocabulary"
destination.mkdir(parents=True, exist_ok=True)
words = {}
book_keys = {}

def key(text):
    return " ".join(text.strip().lower().split())

for book_id in ("GaoZhongluan_2", "CET4luan_2", "CET6_2", "IELTS_3",
                "GaoZhong_3", "CET4_3", "CET6_3", "IELTS_2"):
    entries = []
    path = next((source / book_id).glob("*.json"))
    for line in path.read_text(encoding="utf-8-sig").splitlines():
        if not line.strip():
            continue
        item = json.loads(line)
        word = item["headWord"].strip()
        content = item["content"]["word"]["content"]
        word_key = key(word)
        explanation = "\n".join(
            (meaning.get("pos", "") + ". " + meaning["tranCn"]).strip(". ")
            for meaning in content.get("trans", [])
            if meaning.get("tranCn")
        )
        sentences = content.get("sentence", {}).get("sentences", [])
        example = "\n\n".join(
            "\n".join(filter(None, [sentence.get("sContent"), sentence.get("sCn")]))
            for sentence in sentences[:2]
            if sentence.get("sContent")
        )
        candidate = {"Word": word, "Explanation": explanation, "Example": example, "Forms": ""}
        if word_key not in words:
            words[word_key] = candidate
        else:
            for field in ("Explanation", "Example"):
                if not words[word_key][field] and candidate[field]:
                    words[word_key][field] = candidate[field]
        if word_key not in entries:
            entries.append(word_key)
    book_keys[book_id] = entries

gaokao = []
for item in json.loads((source / "GaoKao_3500.json").read_text(encoding="utf-8-sig")):
    word = item["name"].strip()
    word_key = key(word)
    if word_key not in gaokao:
        gaokao.append(word_key)
    if word_key not in words:
        words[word_key] = {
            "Word": word,
            "Explanation": "\n".join(item.get("trans", [])),
            "Example": "",
            "Forms": "",
        }

labels = {"p": "过去式", "d": "过去分词", "i": "现在分词", "3": "第三人称单数", "r": "比较级", "t": "最高级", "s": "复数", "0": "原形"}
dictionary_subset = []
with (source / "ecdict.csv").open(encoding="utf-8-sig", newline="") as handle:
    reader = csv.DictReader(handle)
    csv_fields = reader.fieldnames
    for item in reader:
        word_key = key(item["word"])
        if word_key not in words:
            continue
        target = words[word_key]
        dictionary_subset.append(item)
        if not target["Explanation"]:
            target["Explanation"] = item.get("translation", "").replace("\\n", "\n")
        forms = []
        for change in item.get("exchange", "").split("/"):
            code, _, value = change.partition(":")
            if code in labels and value and key(value) != word_key:
                forms.append(labels[code] + " " + value)
        target["Forms"] = "；".join(forms)

decks = [
    {"Id": "gaokao", "Name": "高考3500", "Words": gaokao},
    {"Id": "cet4", "Name": "大学英语四级", "Words": book_keys["CET4luan_2"]},
    {"Id": "cet6", "Name": "大学英语六级", "Words": book_keys["CET6_2"]},
    {"Id": "ielts", "Name": "雅思", "Words": book_keys["IELTS_3"]},
]
used = set(word for deck in decks for word in deck["Words"])
entries = [word for word_key, word in words.items() if word_key in used]
assert all(word["Explanation"] for word in entries), "An entry is missing its explanation"
payload = json.dumps({"Words": entries, "Decks": decks}, ensure_ascii=False, separators=(",", ":")).encode("utf-8")
with (destination / "catalog.json.gz").open("wb") as handle:
    with gzip.GzipFile(fileobj=handle, mode="wb", mtime=0) as output:
        output.write(payload)
report = {
    "decks": [{"id": deck["Id"], "name": deck["Name"], "words": len(deck["Words"]),
               "with_examples": sum(bool(words[w]["Example"]) for w in deck["Words"]),
               "with_forms": sum(bool(words[w]["Forms"]) for w in deck["Words"])} for deck in decks],
    "unique_words": len(entries),
    "raw_bytes": len(payload),
    "compressed_bytes": (destination / "catalog.json.gz").stat().st_size,
}
(destination / "counts.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
with zipfile.ZipFile(destination / "vocabulary-data-source.zip", "w", compression=zipfile.ZIP_DEFLATED) as archive:
    for book_id in book_keys:
        path = next((source / book_id).glob("*.json"))
        archive.write(path, "tests/vocabulary-source/" + book_id + "/" + path.name)
    for name in ("GaoKao_3500.json", "kajweb-readme.txt", "QWERTY-LICENSE.txt", "ECDICT-LICENSE.txt"):
        archive.write(source / name, "tests/vocabulary-source/" + name)
    output = io.StringIO(newline="")
    writer = csv.DictWriter(output, fieldnames=csv_fields)
    writer.writeheader()
    writer.writerows(dictionary_subset)
    archive.writestr("tests/vocabulary-source/ecdict.csv", output.getvalue())
    archive.write(Path(__file__), "tools/Build-Vocabulary.py")
print(json.dumps(report, ensure_ascii=True))
