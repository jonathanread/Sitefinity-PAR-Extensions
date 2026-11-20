# Sitefinity PAR Extensions

This repository contains a Sitefinity extension and a Sitefinity web application project used to host and run it. The extension adds a custom chat endpoint that can optionally route questions to a search configuration selected by Agentic RAG.

## Projects

- **`SitefinityPar.Extensions`** — .NET Framework 4.8 class library. It registers the custom chat routes and implements the request validation, Agentic RAG configuration lookup, optional prediction-based routing, and handoff to the Sitefinity Agentic RAG controller.
- **`SitefinityWebApp`** — Sitefinity web application project that provides the host application and its Sitefinity configuration.

Both projects are included in `PAR Extensions SitefinityWebApp.slnx` at the repository root.

## Chat flow

At application startup, the extension registers these routes:

- `POST /api/default/AgenticRag/Chat`
- `/customchat/chat`

The controller accepts the Agentic RAG connector's standard `AskRequest`. It validates the request and configured knowledge box, then loads the requested search configuration from the account's knowledge-box details. Successful configuration responses are cached for the duration configured by `AgenticRAGSearchConfigurationCacheDurationMinutes` in the host application's `Web.config` (default: five minutes). If routing is enabled for that configuration, the extension calls the Agentic RAG prediction endpoint and uses a destination allowed by the routing rules; otherwise, it keeps the requested configuration. The request is then passed to the connector's Sitefinity chat implementation.

Outbound Agentic RAG requests use the configured knowledge-box service token, are limited to a two-minute timeout, and share a bounded connection pool. The extension also contains the JSON serialization and response parsing used by the configuration and prediction flows.

## Configuration

Configure the Agentic RAG connection in Sitefinity before using the endpoint:

1. Configure a knowledge box in Sitefinity with its base URL, knowledge-box ID, and service token.
2. Set the `AgenticRAGAccountId` application setting in the host application's `Web.config` to the corresponding account GUID.
3. Optionally set `AgenticRAGSearchConfigurationCacheDurationMinutes` in `Web.config` to a positive whole number of minutes; if omitted, the cache duration is five minutes.
4. Create the requested search configuration in that knowledge box. Configure routing rules there if prediction-based routing is desired.

Do not commit service tokens or other credentials to source control. The extension expects an HTTPS Agentic RAG base URL.

## Requirements and build

- .NET Framework 4.8 and a compatible Sitefinity installation.
- Sitefinity and Agentic RAG connector packages restored from the configured NuGet sources. The extension's package sources are listed in `SitefinityPar.Extensions/nuget.config`.
- Visual Studio with support for `.slnx` solutions, or MSBuild/NuGet tooling compatible with the legacy project files.

Open `PAR Extensions SitefinityWebApp.slnx` from the repository root, restore NuGet packages, and build the solution. Deploy the extension assembly with the Sitefinity host application using the deployment process for your Sitefinity environment.
