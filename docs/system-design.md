# ManualMate System Design

```mermaid
flowchart LR
    U[User] --> API[REST API]

    API --> PG[(PostgreSQL)]
    API --> RD[(Redis)]
    API --> MQ[RabbitMQ]
    API --> FS[File Storage]

    MQ --> W[Worker]
    W --> AI[AI Services]
    W --> VS[(Vector DB)]
    W --> PG

    U -->|ask| API
    API --> VS
    API --> AI
```

## Key Flows

- **Ingestion (async):** upload → save file → transactional outbox → RabbitMQ → worker extracts, chunks, embeds, stores vectors
- **QA (sync):** question → Redis cache → miss → embed question → top-K chunks from Vector DB → LLM answers → cached
