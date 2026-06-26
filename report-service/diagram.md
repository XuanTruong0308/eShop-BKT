# Slide 4: Low-Level System Architecture Diagram (eShop AI Chatbot)

Below is the Mermaid code representing the updated 4-column system architecture for the eShop AI Chatbot, matching the layout and color coding of Slide 4 (Image 1) but using the new tech stack (.NET Core, Blazor, pgvector, Ollama, Groq, Stripe).

```mermaid
flowchart LR
    %% Subgraph columns
    subgraph Col1 ["1. AI & 3rd-Party Integrations"]
        direction TB
        Groq["Groq Cloud LLM<br>(AI Chat Brain)"]
        Ollama["Ollama Container<br>(AI Search Encoder)"]
        Stripe["Stripe Payments<br>(Credit Cards)"]
    end

    subgraph Col2 ["2. Presentation Layer"]
        direction TB
        BlazorUI["eShop Web UI<br>(Blazor Chat Panel)"]
        ChatState["ChatState.cs<br>(Chat Interface State)"]
    end

    subgraph Col3 ["3. Core Backend Services"]
        direction TB
        CatalogAPI["Catalog.API<br>(Product Search)"]
        BasketAPI["Basket.API<br>(Shopping Cart)"]
        OrderingAPI["Ordering.API<br>(Order Processing)"]
    end

    subgraph Col4 ["4. Databases & Storage"]
        direction TB
        Postgres["PostgreSQL + pgvector<br>(Products & AI Vectors)"]
        Redis["Redis Cache<br>(Fast Session Storage)"]
    end

    %% Connections and Flows
    BlazorUI <-->|User Chat Input| ChatState
    ChatState <-->|1. AI Text Queries| Groq
    ChatState -->|2. Search Products| CatalogAPI
    ChatState -->|3. Add items to Cart| BasketAPI
    
    CatalogAPI <-->|Generate Search Vectors| Ollama
    CatalogAPI <-->|Query Product Vectors| Postgres
    
    BasketAPI -->|gRPC Update| OrderingAPI
    OrderingAPI -->|Process Payment| Stripe
    
    CatalogAPI <-->|Cache Results| Redis
    BasketAPI <-->|Cache Active Cart| Redis

    %% Color Styling matching the original Slide 4 template (Image 1) - White Fill via explicit style declarations for Draw.io compatibility
    style Groq fill:#ffffff,stroke:#FFA500,stroke-width:2px,color:#000000
    style Ollama fill:#ffffff,stroke:#FFA500,stroke-width:2px,color:#000000
    style Stripe fill:#ffffff,stroke:#FFA500,stroke-width:2px,color:#000000

    style BlazorUI fill:#ffffff,stroke:#1E90FF,stroke-width:2px,color:#000000
    style ChatState fill:#ffffff,stroke:#1E90FF,stroke-width:2px,color:#000000

    style CatalogAPI fill:#ffffff,stroke:#BA55D3,stroke-width:2px,color:#000000
    style BasketAPI fill:#ffffff,stroke:#BA55D3,stroke-width:2px,color:#000000
    style OrderingAPI fill:#ffffff,stroke:#BA55D3,stroke-width:2px,color:#000000

    style Postgres fill:#ffffff,stroke:#2E8B57,stroke-width:2px,color:#000000
    style Redis fill:#ffffff,stroke:#2E8B57,stroke-width:2px,color:#000000

    style Col1 fill:#ffffff,stroke:#DDDDDD,stroke-dasharray: 5 5,color:#000000
    style Col2 fill:#ffffff,stroke:#DDDDDD,stroke-dasharray: 5 5,color:#000000
    style Col3 fill:#ffffff,stroke:#DDDDDD,stroke-dasharray: 5 5,color:#000000
    style Col4 fill:#ffffff,stroke:#DDDDDD,stroke-dasharray: 5 5,color:#000000
```

---

## Instructions to import into Draw.io:
1. Open [Draw.io](https://app.diagrams.net/).
2. Select **Arrange** -> **Insert** -> **Advanced** -> **Mermaid**.
3. Copy the Mermaid block above and paste it into the textbox.
4. Click **Insert** to generate editable boxes, styling, and structure that matches the template perfectly!
