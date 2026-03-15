# Config files

(limits.json):

```
{
  "MaxConversationLength": 500,
  "SummaryDivisionFactor": 10,
  "MaxMessageLength": 2000,
  
  "Memory": {
    "MaxShortTermEntries": 20,
    "MaxLongTermEntries": 500,
    "SummaryDivisionFactor": 10
  }
}

```

(model.json):

```
{
  "ModelName": "qwen3:8b",
  "MaxTokens": 1024,
  "ResponseTimeoutSeconds": 30,
  "KeepAliveMinutes": 10
}

```

(settings.json):

```
{
  "EnableSummaries": true,
  "EnableEmojiLogging": true,
  "AutoRepairOnStart": true
}

```

