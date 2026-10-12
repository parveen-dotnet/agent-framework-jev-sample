# Agent Framework + JEV (TypeSafeAI) + Azure AI Foundry Sample

A minimal .NET sample built on the **Microsoft Agent Framework** demonstrating:

- A combined **Manager/Orchestrator** that receives a free-text task and decides
  whether it should go to a **Dev** agent or a **QA** agent.
- The routing decision is made by **JEV**, TypeSafeAI's typed "System One" decision
  model (https://docs.typesafe.ai) — a structured `Choice` question instead of an
  LLM prompt you'd have to parse.
- **Dev** and **QA** are real Microsoft Agent Framework agents, backed by either:
  - an **Azure AI Foundry** model deployment (`AIProjectClient.AsAIAgent(...)`), or
  - a **local OpenAI-compatible server** such as **LM Studio** or Ollama
    (`OpenAIClient(...).GetChatClient(...).AsAIAgent(...)`) — useful for running
    fully offline against a local model (e.g. `qwen/qwen3-1.7b` in LM Studio).
- If nothing is configured, the sample falls back to clearly-labeled offline mocks
  so you can still explore the flow.

## Project layout

```
Jev/
  IDecisionEngine.cs        - typed decision engine abstraction (Choice-only, matches JEV's API)
  JevDecisionEngine.cs       - real HttpClient client for POST https://api.typesafe.ai/v1/systemone, configured via IOptions<TypeSafeAIOptions>
  MockDecisionEngine.cs      - offline fallback (naive keyword scoring), used only without an API key
  DecisionModels.cs          - request/response DTOs matching JEV's documented JSON shape
  TypeSafeAIOptions.cs       - strongly-typed options bound from the "TypeSafeAI" config section
Agents/
  ITaskAgent.cs              - "handle a task, return text" abstraction
  FoundryAgentFactory.cs     - builds Dev/QA agents either on Azure AI Foundry or on a local
                               OpenAI-compatible server (LM Studio/Ollama), via Microsoft Agent Framework
  AzureAIFoundryOptions.cs   - strongly-typed options bound from the "AzureAIFoundry" config section
  LocalOpenAIOptions.cs      - strongly-typed options bound from the "LocalOpenAI" config section
  MockTaskAgent.cs           - offline fallback agent, used only without Foundry/local config
  ManagerOrchestrator.cs     - the Manager/Orchestrator: calls JEV, routes to Dev or QA, returns result
Program.cs                   - console entry point: builds IConfiguration + a small DI container, resolves IOptions<T>, wires everything up
appsettings.json              - non-secret placeholders only
```

## Configuration (no secrets committed)

Configuration is read from **appsettings.json** via `Microsoft.Extensions.Configuration`,
bound into strongly-typed classes (`TypeSafeAIOptions`, `AzureAIFoundryOptions`,
`LocalOpenAIOptions`) and consumed through **`IOptions<T>`** (`Microsoft.Extensions.Options`)
— not raw `IConfiguration` indexers and not environment variables. `Program.cs` builds a small
`ServiceCollection`/`ServiceProvider` purely to register
`services.Configure<TOptions>(configuration.GetSection(...))` and resolve the `IOptions<T>`
instances, which are then passed into `JevDecisionEngine` and
`FoundryAgentFactory.CreateDevAndQaAgents(...)` / `CreateLocalDevAndQaAgents(...)` respectively.

Layering, in increasing precedence:

1. `appsettings.json` — committed, non-secret placeholders only.
2. `appsettings.Development.json` — gitignored, put your real local values here.
3. `dotnet user-secrets` — alternative to #2 for storing the API key outside the repo folder.

| Setting | Purpose | Required for real behavior? |
|---|---|---|
| `TypeSafeAI:ApiKey` | JEV (TypeSafeAI) API key from https://console.typesafe.ai/keys | Yes, else mock decision engine is used |
| `TypeSafeAI:Endpoint` | Overrides the JEV API endpoint (default `https://api.typesafe.ai/v1/systemone`) | Optional |
| `LocalOpenAI:Endpoint` | Local OpenAI-compatible server URL, e.g. `http://127.0.0.1:1234/v1` for LM Studio | Checked first; if set, Dev/QA use this and Azure AI Foundry is skipped |
| `LocalOpenAI:ModelId` | Model id as exposed by the local server, e.g. `qwen/qwen3-1.7b` | Optional (default `local-model`) |
| `LocalOpenAI:ApiKey` | Placeholder API key (most local servers don't validate it) | Optional (default `lm-studio`) |
| `AzureAIFoundry:Endpoint` | Your Azure AI Foundry project endpoint, e.g. `https://<project>.services.ai.azure.com` | Used only if `LocalOpenAI:Endpoint` isn't set; else mock Dev/QA agents are used |
| `AzureAIFoundry:ModelDeployment` | Model deployment name (default `gpt-4o-mini`) | Optional |

Dev/QA provider precedence in `Program.cs`: **`LocalOpenAI:Endpoint`** (if set) →
**`AzureAIFoundry:Endpoint`** (if set) → mock fallback.

Azure AI Foundry auth uses `DefaultAzureCredential` (e.g. `az login` for local dev).
For production, replace with a specific credential such as `ManagedIdentityCredential`.

> **Why a separate path for local models?** `DefaultAzureCredential` attaches an Azure AD
> **Bearer** token and refuses to do so over a non-HTTPS endpoint (e.g. LM Studio's default
> `http://127.0.0.1:1234`), raising *"Bearer token authentication is not permitted for
> non TLS protected (http) endpoints"*. Local servers like LM Studio/Ollama don't use Azure AD
> anyway — they expose an OpenAI-compatible API — so `CreateLocalDevAndQaAgents` instead uses
> the OpenAI SDK's plain `ApiKeyCredential`, which works fine over plain HTTP.

### Example: appsettings.Development.json (gitignored) — local LM Studio model

```json
{
  "TypeSafeAI": {
    "ApiKey": "sk-..."
  },
  "LocalOpenAI": {
    "Endpoint": "http://127.0.0.1:1234/v1",
    "ModelId": "qwen/qwen3-1.7b"
  }
}
```

### Example: appsettings.Development.json (gitignored) — Azure AI Foundry

```json
{
  "TypeSafeAI": {
    "ApiKey": "sk-..."
  },
  "AzureAIFoundry": {
    "Endpoint": "https://my-project.services.ai.azure.com"
  }
}
```

### Example: dotnet user-secrets (alternative to appsettings.Development.json)

```powershell
dotnet user-secrets set "TypeSafeAI:ApiKey" "sk-..."
dotnet user-secrets set "LocalOpenAI:Endpoint" "http://127.0.0.1:1234/v1"
dotnet user-secrets set "LocalOpenAI:ModelId" "qwen/qwen3-1.7b"
dotnet run
```


## Running

```powershell
dotnet build
dotnet run
```

Then type a task, e.g.:

```
> Fix the null reference exception on the login page
> Write test cases to verify the checkout flow
> exit
```

For each task the console prints:
- JEV's chosen agent (`dev`/`qa`), its confidence, and the full probability distribution.
- The chosen agent's response (real model output from your local server or Azure AI
  Foundry, or a mock message if neither is configured).

## Notes / Caveats

- JEV has no official .NET SDK yet (only Python), so `JevDecisionEngine` calls the
  documented REST API directly with `HttpClient`.
- `MockDecisionEngine` and `MockTaskAgent` exist purely so the sample is runnable
  without any cloud/local credentials; they are intentionally naive and not a
  substitute for the real JEV model or a real model-backed agent.
- The Manager and Orchestrator are combined into a single `ManagerOrchestrator`
  class per this sample's scope; splitting them into separate agents/services is a
  straightforward extension if you want a distinct top-level router.
- Local model responses depend entirely on the local model's quality (e.g.
  `qwen/qwen3-1.7b` is a small model) — expect less polished output than a larger
  hosted model.
