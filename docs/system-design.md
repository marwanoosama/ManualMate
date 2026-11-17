# ManualMate System Design

```mermaid
flowchart TB
    Client["Client"]

    subgraph API["Backend API"]
        direction LR
        REST["REST Controllers"]
        SVC["Core Services"]
        BG["Background Workers"]
    end

    subgraph EXT["External Systems"]
        DB[("Database<br/>metadata")]
        VDB[("Vector Store<br/>chunks + embeddings")]
        RD[("Cache")]
        MQ{{"Message Broker"}}
        AI["AI Services<br/>Embedding model • LLM"]
        FS["File Storage"]
    end

    %% Ingestion (async)
    Client --> REST --> SVC
    SVC -->|"save file"| FS
    SVC -->|"tx: metadata + outbox row"| DB
    BG -->|"poll outbox"| DB
    BG -->|"publish event"| MQ
    BG -->|"consume"| FS
    BG -->|"extract → chunk → embed"| AI
    BG -->|"store chunks + vectors"| VDB

    %% QA (sync)
    REST --> SVC2["Query Service"]
    SVC2 -->|"cache check"| RD
    SVC2 -->|"query embedding"| AI
    SVC2 -->|"top-K chunks"| VDB
    SVC2 -->|"context + question"| AI
```

## Key Flows

- **Ingestion (async):** file upload → save → transactional outbox → broker → worker extracts, chunks, embeds, stores vectors
- **QA (sync):** question → cache → miss → embed question → retrieve top-K chunks → LLM answers → cached
