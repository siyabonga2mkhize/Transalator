# Azure Translator Web App (ASP.NET Core 8)

A minimal web app with a UI to call Azure Translator (2025-05-01-preview). It proxies requests through the server to keep your key safe, and shows:

- Translated text (from response body)
- Characters charged (response header)
- Source tokens charged (response header, when available)
- Target tokens charged (response header, when available)
- Response time
- x-requestid

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

## Notes
- The server extracts common headers: `x-metered-usage` for character counts, `x-ai-invoke-usage` (when present) for token counts, and `x-requestid`.
- Token headers may vary across previews; the app shows `n/a` if not returned.
- The UI lets you optionally pick a model deployment, gender, and tone.
