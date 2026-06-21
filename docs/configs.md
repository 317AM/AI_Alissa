# Config files

(indexing_rules.json):

```
{
  "rebuildOnAccess": true,
  "usePartialIndexing": true,
  "applyForgetfulness": true,
  "decayAfterDays": 7,
  "decayRatePerDay": 0.05,
  "forgettingThreshold": 0.1,
  "indexingFilters": [],
  "maxIndexSize": 500,
  "useHeuristicScoring": true,
  "accessCountForConsolidation": 3
}

```

(limits.json):

```
{
  "MaxConversationLength": 500,
  "SummaryDivisionFactor": 10,
  "MaxMessageLength": 2000,
  
  "Memory": {
    "MaxShortTermEntries": 40,
    "MaxLongTermEntries": 500,
    "SummaryDivisionFactor": 10
  }
}

```

(logging.json):

```
{
  "enableConversationLogs": true,
  "enableSummaryLogs": true,
  "enableMemoryLogs": true,
  "enableDebugLogs": false,
  "enablePromptLogs": false,
  "logsDirectory": "logs",
  "minLogLevel": "Info",
  "maxLogFileSizeMb": 10,
  "logFormat": "Simple"
}

```

(memory_rules.json):

```
{
  "maxShortTermEntries": 20,
  "maxLongTermEntries": 200,
  "importanceThreshold": 0.6,
  "compressionFactor": 0.4
}

```

(model.json):

```
{
  "ModelName": "qwen3:14b",
  "MaxTokens": 2048,
  "ResponseTimeoutSeconds": 60,
  "KeepAliveMinutes": 30
}

```

(persona.json):

```
{
  "version": 1,
  "createdUtc": "2024-01-01T00:00:00Z",
  "updatedUtc": "2024-01-01T00:00:00Z",
  "current_user": {
    "name": "",
    "preferences": [],
    "known_info": {}
  },
  "appearance": {
    "description": "",
    "mood": "",
    "attributes": {}
  },
  "current_code": {
    "name": "",
    "language": "",
    "task": "",
    "snippet": "",
    "related_concepts": []
  },
  "custom_fields": {}
}

```

(personality_rules.json):

```
{
  "emotionalStyle": "Unfiltered",
  "enableCatgirlTraits": true,
  "sillinessLevel": 0.7,
  "unfilteredMode": true,
  "enableQuirks": true,
  "primaryFocus": "Coding",
  "correctionBehavior": "Grateful",
  "customTraits": {
    "species": "catgirl",
    "age_personality": "teenage",
    "primary_trait": "annoying",
    "secondary_trait": "silly"
  },
  "memoryBehavior": {
    "acknowledgeMemory": true,
    "simulateForgetfulness": false,
    "userProfileReferenceBehavior": "Natural",
    "contextContinuity": true
  }
}

```

(prompt_rules.json):

```
{
  "maxPromptTokens": 4096,
  "maxSessionMessages": 10,
  "maxMemoryEntries": 15,
  "trimPriority": [
    "Identity",
    "UserProfile",
    "Facts",
    "RecentContext",
    "Skills",
    "SystemLearnings",
    "SessionCache"
  ],
  "includeMediumTermMemory": false,
  "includePersonaFields": true,
  "tokensPerLine": 4
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

