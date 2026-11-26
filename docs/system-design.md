# ManualMate System Design

```mermaid
flowchart LR
    U[User] --> API[REST API]

    API --> PG[(PostgreSQL + pgvector)]
    API --> RD[(Redis)]
    API --> FS[File Storage]

    API -->|enqueue job| HF[Hangfire]
    HF --> J[Processing Job]
    J --> AI[AI Services]
    J --> PG

    U -->|ask| API
    API --> AI
```

## Key Flows

- **Ingestion (async):** upload → save item + file → enqueue Hangfire job → job extracts, chunks, embeds, stores vectors
- **QA (sync):** question → Redis cache → miss → embed question → top-K chunks from pgvector → LLM answers → cached
