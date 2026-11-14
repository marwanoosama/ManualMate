# ManualMate API — Bruno Collection

All API endpoints as a [Bruno](https://www.usebruno.com/) collection (files on disk, Git-friendly).
Location matters: it lives inside `ManualMate.API/` so the PDF file paths (`../wwwroot/Files/...`)
resolve relative to this folder. If you move the collection, re-pick the files in the file fields.

## How to use

1. Start the API:
   ```bash
   cd ManualMate/ManualMate/ManualMate.API
   dotnet run --launch-profile http
   ```
2. Open Bruno → **Open Collection** → select this `bruno/` folder.
3. Pick the **`Local`** environment (top-right dropdown). It defines:
   - `host` — `http://localhost:5035`
   - `itemId` — pre-set to the **LG Microwave** item (already processed & askable)
   - `question` — the question sent by "Ask question (RAG)"
4. After **Create item + upload manual**, grab the new item's id from *Get all items*
   and paste it into the `itemId` environment variable before running the id-based requests.

## Suggested order

| Seq | Request | What it does / expects |
|---|---|---|
| 1 | Get all items | 200 — list |
| 2 | Get item by id | 200 — single item |
| 3 | Ask question (RAG) | 200 — Gemini answer from the manual; 2nd identical ask is served from Redis (~15 ms) |
| 4 | Create item + upload manual | 200 — returns immediately; processing runs async (watch Status go Pending → Processing → Completed in the DB or *Get all items*). The LG manual ≈ 1 min |
| 5 | Update item | 200 — renames + edits description |
| 6 | Replace manual file | 200 — replaces the stored PDF only; embeddings are NOT regenerated (see PROJECT-GUIDE.md) |
| 7 | Delete item | 200 — removes item + embeddings (the PDF file stays on disk) |
| 8 | Edge — item not found | 404 |
| 9 | Edge — create without file | 400 — "File is required" |
| 10 | Edge — wrong file type | 400 — .txt is rejected |
| 11 | Edge — oversized file | 400 — 52 MB file is rejected by the app's own 50 MB limit (Kestrel allows up to 60 MB) |

## CLI

```bash
cd ManualMate/ManualMate.API/bruno
npx @usebruno/cli run --env Local
# or a single request:
npx @usebruno/cli run ask-question.bru --env Local
```
