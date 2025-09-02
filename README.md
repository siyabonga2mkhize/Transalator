# Azure Translator Web App 

The new Azure AI Translator API provides you with flexibility to choose general neural machine translation (NMT), or a list of generative AI large language models (LLMs) at a request level. During preview the API supports general NMT, gpt-4o and gpt-4o-mini models. Using generative AI models, the new API offers new capabilities to produce tone translation, gender translation, and adaptive custom translation. Languages supported are [listed here](https://aka.ms/translatorlanguages).

This .NET web app (ASP.NET Core 8) lets users enter text, select a target language, and add optional inputs. After clicking ‘Translate’, it uses the Azure AI Translator API (2025-05-01-preview) to return the translated text, charges, and response time on the UI.

## Configure

Set your Azure Translator resource details in `appsettings.json` or via environment variables:

- AzureTranslator:Endpoint  (e.g., `https://<your-resource-name>.cognitiveservices.azure.com`)
- AzureTranslator:Key
- AzureTranslator:Region
- AzureTranslator:ApiVersion (defaults to `2025-05-01-preview`)

Environment variable examples (Windows PowerShell):

```powershell
$env:AzureTranslator__Endpoint = 'https://<your>.cognitiveservices.azure.com'
$env:AzureTranslator__Key = '<your-key>'
$env:AzureTranslator__Region = '<your-region>'
$env:AzureTranslator__ApiVersion = '2025-05-01-preview'
```

## Run

```powershell
# from the project folder
dotnet restore
dotnet run
```

Open the printed URL (e.g., http://localhost:5089) and use the UI.

