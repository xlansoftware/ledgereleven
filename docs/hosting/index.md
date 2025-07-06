# Hosting Locally

This document describes how to host the LedgerEleven application locally using the provided Docker Compose setup.

## Prerequisites

*   [Docker](https.docs.docker.com/get-docker/)
*   [Docker Compose](https://docs.docker.com/compose/install/)

## Staging Environment

The `.devops/stage` directory contains a `docker-compose.yaml` file that is configured to run the application in a staging environment.

### Configuration

Before you can run the application, you need to create a `.env` file in the `.devops/stage` directory. You can use the provided `.env.template` as a starting point:

```bash
cp .devops/stage/.env.template .devops/stage/.env
```

Next, you need to fill in the values for the following environment variables in the `.env` file:

*   `APP_TUNNEL_TOKEN`
*   `AI_API_KEY`

### Cloudflare Tunnel

The application uses a [Cloudflare Tunnel](https://www.cloudflare.com/products/tunnel/) to expose the locally running application to the internet. This is one convenient way to obtain an HTTPS certificate for your local development environment. While not the only method, it's fast and easy, though it does require you to own a domain. This setup is particularly useful for testing webhooks or sharing the application with others.

To get a Cloudflare Tunnel token, you will need to:

1.  Sign up for a Cloudflare account.
2.  Follow the instructions to [create a tunnel](https://developers.cloudflare.com/cloudflare-one/connections/connect-apps/install-and-setup/tunnel-guide/).

### AI API Key

The application uses an AI API to provide AI-powered features. While the examples often refer to OpenAI, the application is designed to work with any AI provider that is compatible with the OpenAI API. You can provide the URL and API key for any such provider.

To get an OpenAI API key (as an example):

1.  Sign up for an OpenAI account.
2.  Navigate to the [API keys](https://platform.openai.com/account/api-keys) section of your account to create a new secret key.

Alternatively, you can use a local Large Language Model (LLM) for development and testing. Tools like [LM Studio](https://lmstudio.ai/) allow you to run various LLMs (e.g., Llama models) directly on your machine, providing an OpenAI-compatible API endpoint. This can be a great option for privacy and cost-effectiveness during development.

To learn more about running local LLMs:

*   [LM Studio](https://lmstudio.ai/)
*   [Ollama](https://ollama.com/) (another popular tool for running local LLMs)

## Running the Application

Once you have configured the `.env` file, you can run the application using the following command from the `.devops/stage` directory:

```bash
docker-compose up -d
```

This will start the application in the background. You can view the logs using the following command:

```bash
docker-compose logs -f
```
