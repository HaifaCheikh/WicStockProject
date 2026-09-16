# 🤖 Multi-Agent AI Architecture

The `ai-service` runs on a **4-agent decision layer**, backed by a security guard and two internal services — all coordinated by a central orchestrator:

```
                            User Query
                                 |
                       +-------------------+
                       | OrchestratorAgent |
                       +-------------------+
                                 |
          +----------------------+----------------------+
          v                      v                      v
+-------------------+  +-------------------+  +-------------------+
|    NL2SQLAgent    |  |   SurstockAgent   |  |  PreferenceAgent  |
+-------------------+  +-------------------+  +-------------------+
          v                      v                      v
+-------------------+  +-------------------+  +-------------------+
|   SQLGuardAgent   |  |SurstockDataFetcher|  |   ChartBuilder    |
+-------------------+  +-------------------+  +-------------------+
          |
          v
     SQL Server
```

## Responsibilities

| Component | Role |
|---|---|
| **OrchestratorAgent** | Routes each incoming request to the right specialized agent |
| **NL2SQLAgent** | Converts natural-language questions into SQL queries |
| **SurstockAgent** | Diagnoses overstock/shortage risk scenarios |
| **PreferenceAgent** | Handles chart and output customization |
| **SQLGuardAgent** | Enforces SELECT-only validation and RBAC before any query touches `SQL Server` |
| **SurstockDataFetcher** | Internal service supporting overstock diagnostics |
| **ChartBuilder** | Internal service generating dynamic visualizations |

## Technology

- Python 3.10+, FastAPI
- Ollama (`qwen3:1.7b`) for local LLM inference
- ChromaDB for RAG (Retrieval-Augmented Generation), embedded/persistent mode
- `pyodbc` for SQL Server connectivity

---

⬅ [Back to README](../README.md)
