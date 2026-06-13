# Memory Flow & Pipeline

## Complete Memory Journey

### Phase 1: Conversation

User and Alissa have a conversation. All messages go into session cache:
```
User: "What's C#?"
Alissa: "C# is a modern, type-safe language..."
```

**Storage**: `memory/short_term/session_cache.json`
- Contains: Only Message objects (role, content, timestamp)
- Limit: MaxShortTermEntries (default: 20)
- Purpose: Immediate context for current session

### Phase 2: Session Ends

When user types "exit" or application closes, SaveConversation is called.

### Phase 3: Summary Generation

SummaryGenerationService generates 3 outputs:

1. **Summary** (5 lines)
   ```
   "Discussed C# fundamentals including types, async/await, and 
   LINQ. User showed interest in practical patterns. Clarified 
   memory model and reference types."
   ```

2. **Highlights** (key points)
   ```
   - C# is strongly typed
   - Async/await for concurrent programming
   - LINQ for data queries
   ```

3. **Topics** (identified topics)
   ```
   ["C#", "Types", "Async", "LINQ"]
   ```

**Key Feature**: This service is completely isolated:
- Uses only SummaryGenerationService and MemoryExtractionService
- Never touches AlissaClient
- Never touches session cache
- Generation queries DON'T pollute memory

### Phase 4: Memory Extraction

MemoryExtractionService sends summary to model for structured extraction:

**Extraction Prompt**:
```
Based on the following conversation summary, extract structured memory 
in JSON format. Return ONLY valid JSON, no other text.

Extract into these categories:
- user_profile: User information (name, preferences, goals, background)
- facts: General knowledge and facts discussed
- skills: Capabilities and techniques mentioned
- system_learnings: What you learned about how to help better

[Summary text...]
```

**Model Response** (expected JSON):
```json
{
  "user_profile": {
	"learning_style": "prefers practical examples",
	"interest": "C# programming"
  },
  "facts": {
	"csharp_typing": "strongly typed language",
	"async_definition": "concurrent task-based programming"
  },
  "skills": {
	"linq_mastery": "user comfortable with LINQ queries"
  },
  "system_learnings": {
	"improvement": "explain memory model before diving to async"
  }
}
```

**Flexible Extraction**:
- If JSON malformed: Accept partial data
- If parsing fails: Log and continue with empty result
- No strict validation - graceful degradation

### Phase 5: Memory Storage

Extracted data distributed to appropriate long-term stores:

**user_profile.json**:
```json
[
  {
	"key": "learning_style",
	"value": "prefers practical examples",
	"relevance": 0.9,
	"isCoreMemory": false,
	"timestamp": "2024-01-15T10:30:00Z"
  },
  ...
]
```

**facts.json**:
```json
[
  {
	"key": "csharp_typing",
	"value": "strongly typed language",
	"relevance": 0.8,
	"isCoreMemory": false,
	"timestamp": "2024-01-15T10:30:00Z"
  },
  ...
]
```

**skills.json**, **system_learnings.json**: Similar structure

**Relevance Scores**:
- user_profile: 0.9 (very important)
- facts: 0.8 (important)
- skills: 0.85 (important)
- system_learnings: 0.95 (highest, improve interactions)

### Phase 6: Medium-Term Memory (Optional)

If enabled in config (`includeMediumTermMemory: true`):

**recent_context.json**:
```json
[
  {
	"sessionId": "abc-123",
	"timestamp": "2024-01-15T10:30:00Z",
	"summary": "Discussed C# fundamentals...",
	"relevanceScore": 0.65,
	"messageCount": 12,
	"topics": ["C#", "Types", "Async", "LINQ"],
	"highlights": ["C# is strongly typed", ...],
	"tags": ["coding", "csharp", "patterns"]
  }
]
```

**Purpose**: Quick access to recent sessions without hitting long-term storage
**Limits**: Auto-prunes oldest/lowest relevance entries
**Injection**: Can be included in system prompt for immediate context

### Phase 7: Index Rebuilding

MemoryIndexBuilder creates index from all memory:

**memory_index.json**:
```json
{
  "version": 1,
  "lastBuiltUtc": "2024-01-15T10:35:00Z",
  "totalMemories": 47,
  "entries": {
	"UserProfile_learning_style": {
	  "key": "learning_style",
	  "category": "UserProfile",
	  "relevance": 0.9,
	  "timestamp": "2024-01-15T10:30:00Z",
	  "isCoreMemory": false,
	  "tags": ["learning", "style"]
	},
	...
  },
  "allTags": ["learning", "style", "csharp", "async", ...],
  "allTopics": ["C#", "Types", "Async", "LINQ"]
}
```

**"Dumb" Indexing Features**:
- **Lazy Rebuild**: Rebuilt every access (wasteful)
- **Heuristic Scoring**: Search uses heuristic matching, not optimal
- **Relevance Decay**: Old memories lose relevance over time
- **Forgetting**: Memories below threshold removed from index
- **Limited Index**: Keeps only top N most relevant

### Phase 8: Prompt Injection

When next conversation starts, PromptBuilder injects memory:

**Injection Order** (priority):
```
1. ## Identity
   "Name: Alissa, Age: 17, Species: catgirl..."

2. ## Behavior
   "Be playful and analytical..."

3. ## Boundaries
   "Stay focused on coding..."

4. ## User Profile
   "- learning_style: prefers practical examples
	- interest: C# programming"

5. ## Known Facts
   "- csharp_typing: strongly typed language
	- async_definition: concurrent programming"

6. ## Recent Context
   "[User]: How do I use LINQ?
	[Alissa]: LINQ is..."

7. ## Skills
   "- linq_mastery: user comfortable with LINQ"

8. ## System Learnings
   "- improvement: explain memory model first"
```

**Token Budgeting**:
If total tokens exceed MaxPromptTokens (4096):
- Keep sections 1-3 (always required)
- Trim section 8 first (lowest priority)
- Then 7, 6, 5, etc. until under budget

**Result**: System prompt ready for next message to model

## Memory Lifecycle

```
New Conversation
	↓
Session Cache (short-term, immediate)
	↓
[Session ends]
	↓
Summary Generation (isolated, no cache pollution)
	↓
Memory Extraction (flexible, graceful failures)
	↓
├─→ User Profile (long-term)
├─→ Facts (long-term)
├─→ Skills (long-term)
├─→ System Learnings (long-term)
└─→ Medium-Term Memory (optional, recent context)
	↓
Index Building (lazy, heuristic)
	↓
Next Conversation
	↓
PromptBuilder injects memory
	↓
System prompt ready
```

## Key Principles

1. **Session Cache Purity**: Only stores messages, never summaries or metadata
2. **Isolated Summarization**: Summary generation doesn't affect session or memory
3. **Flexible Extraction**: Gracefully handles incomplete/malformed JSON
4. **Lazy Indexing**: Index rebuilt on access, not stored permanently
5. **Smart Injection**: Token budget enforced, priority-based trimming
6. **Optional Medium-Term**: Can be disabled if not needed

## Performance Considerations

- **Summary Generation**: Async, single model call per session
- **Memory Extraction**: Async, single model call per summary
- **Index Rebuilding**: Synchronous, loads all memory (intentionally wasteful)
- **Prompt Building**: Synchronous, reads files and combines sections
- **Memory Injection**: Filter and sort existing memories, minimal overhead

## Troubleshooting

**Session Cache getting large**: Check MaxShortTermEntries config
**Memory extraction not working**: Enable EnableMemoryLogs, check model response
**Index seems to forget too quickly**: Adjust DecayRatePerDay and DecayAfterDays
**Prompts too long**: Lower MaxPromptTokens or reduce MaxMemoryEntries
**Missing memories in prompt**: Check token budget being exceeded, verify extraction
