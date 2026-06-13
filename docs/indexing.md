# Indexing Strategy

## Overview

Alissa's memory indexing is intentionally "dumb" and inefficient. It's designed to be more human-like (forgetful, imperfect) rather than optimal and efficient.

## Why "Dumb"?

Traditional indexing prioritizes:
- Speed (quick lookup)
- Completeness (remember everything)
- Accuracy (optimal results)

Human memory prioritizes:
- Interesting connections
- Recent events
- Frequently accessed items
- Forgetting irrelevant things

Alissa's indexing does the latter.

## Lazy Rebuild on Access

### How It Works

Every time memory is accessed, the entire index is rebuilt from scratch:

```
User: "Do you remember...?"
	↓
PromptBuilder calls MemoryIndexBuilder.GetIndex()
	↓
Check if cache valid? NO (always false with lazy rebuild)
	↓
Load ALL memories from disk
	↓
Filter and score each memory
	↓
Apply decay to old memories
	↓
Prune to max size
	↓
Return index
```

### Performance Impact

- **Wasteful**: Rebuilds even if nothing changed
- **Redundant**: Full scan every access
- **Slow**: No caching of results

### Why?

This simulates human memory not being optimized. You don't have a perfect index - you search through memories each time.

## Heuristic Scoring

### Optimal Search (Traditional)

```
Query: "coding"
	↓
Search all entries for exact/semantic match
	↓
Return most relevant first
	↓
Result: Perfect accuracy, fast
```

### Heuristic Search (Alissa's Way)

```
Query: "coding"
	↓
Split into words: ["coding"]
	↓
For each memory:
  - Does key contain word? +0.3
  - Do tags contain word? +0.15
  - Recency bonus? +0.1
  - Is core memory? +0.2
	↓
Sort by score
	↓
Return top N
	↓
Result: Good enough, sometimes miss obvious, sometimes find hidden
```

### Heuristic Scoring Advantages

- Makes search results more "interesting" (not always optimal)
- Can discover tangential connections
- Feels more like real memory search
- Cheaper than semantic search (no embedding model)

## Relevance Decay & Forgetting

### How Decay Works

Memories lose relevance over time based on age:

```
Created: 7 days ago, relevance: 0.9
	↓
Today (decay_after_days = 7)
	↓
age_in_days = 7 (exactly at threshold)
	↓
decay_periods = 0 (no decay yet)
	↓
relevance = 0.9 * (0.95^0) = 0.9 (unchanged)
```

```
Created: 14 days ago, relevance: 0.9
	↓
Today
	↓
age_in_days = 14
	↓
decay_periods = 14 - 7 = 7
	↓
relevance = 0.9 * (0.95^7) = 0.9 * 0.695 = 0.625
```

### Forgetting Threshold

Memories with relevance < `forgettingThreshold` (default 0.1) are removed from index:

```
Old memory, very decayed: relevance 0.05
	↓
Is relevance < 0.1? YES
	↓
Remove from index (forgotten)
```

### Configuration

In `config/indexing_rules.json`:

```json
{
  "applyForgetfulness": true,           // Enable decay
  "decayAfterDays": 7,                  // Start forgetting after 7 days
  "decayRatePerDay": 0.05,              // 5% decay per day
  "forgettingThreshold": 0.1            // Forget if below 0.1
}
```

### Example Timeline

Day 0: Memory created, relevance = 0.9
Day 7: Still relevant, relevance = 0.9 (no decay yet)
Day 8: Decay starts, relevance = 0.855
Day 15: relevance = 0.62
Day 30: relevance = 0.20
Day 35: relevance = 0.08 (below 0.1, forgotten)

## Partial Indexing

### What It Means

Don't index every single memory. Only index:
- Recent memories
- High relevance memories
- Core memories (always keep)
- Frequently accessed memories

### Configuration

```json
{
  "usePartialIndexing": true,     // Only index relevant ones
  "maxIndexSize": 500,            // Keep only top 500
  "accessCountForConsolidation": 3 // Keep frequently accessed
}
```

### How It Works

1. Collect all memories
2. Sort by relevance + recency
3. Keep only top `maxIndexSize`
4. Remove least relevant

Result: Index contains best memories, older stuff naturally forgotten.

## Core Memory Exemption

Some memories are marked as "core" and:
- Never decay
- Never forgotten
- Always in index

Example core memories:
- User's name
- Critical facts
- Important learnings

Configuration:
```csharp
new MemoryEntry("user_name", "Jackson", 1.0, true)  // true = core
```

## Index Size Management

### Problem

If you keep every memory, index grows unbounded.

### Solution

Three-tier approach:

1. **During storage**: Prune old/weak memories
2. **During indexing**: Keep only top N (maxIndexSize)
3. **During access**: Apply decay and forgetting

### Example

```
Storage: 10,000 memories
	↓
Apply 6-month retention: 8,000
	↓
Index: Keep top 500
	↓
Search: Score and filter
	↓
Result: Manageable, relevant memories
```

## Real-World Behavior

### Perfect Recall (Not Alissa)

```
User: "What was my name again?"
Model: "It's Jackson. You told me on day 1."
Result: Always correct, boring
```

### Human-Like Recall (Alissa)

```
Day 1: User: "I'm Jackson"
	↓
Day 30: User: "What's my name?"
	↓
Index rebuild:
  - Jackson memory exists
  - Created 30 days ago
  - Relevance decayed: 0.9 → 0.20
  - Still in index (above threshold)
  - Ranked in results (but not #1 if other memories newer)
	↓
Alissa: "You're Jackson! Or wait... did you say that on day 1? Yeah."
Result: Correct, but with uncertainty (human-like)
```

## Configuration for Different Personalities

### Forgetful Alissa
```json
{
  "decayAfterDays": 1,
  "decayRatePerDay": 0.2,
  "forgettingThreshold": 0.3,
  "maxIndexSize": 100
}
```
Forgets things in days, small active memory

### Genius Alissa
```json
{
  "rebuildOnAccess": false,
  "applyForgetfulness": false,
  "useHeuristicScoring": false,
  "usePartialIndexing": false
}
```
Perfect memory, cached index, optimal search

### Typical Alissa
```json
{
  "rebuildOnAccess": true,
  "applyForgetfulness": true,
  "decayAfterDays": 7,
  "decayRatePerDay": 0.05,
  "useHeuristicScoring": true,
  "maxIndexSize": 500
}
```
Human-like: forgetful, heuristic, lazy

## Performance Implications

| Strategy | Speed | Memory Use | Accuracy | Human-Like |
|----------|-------|-----------|----------|-----------|
| **Lazy Rebuild** | Slow | Low | Perfect | Yes ✓ |
| **Cached Index** | Fast | Medium | Perfect | No |
| **Heuristic Search** | Medium | Low | Good | Yes ✓ |
| **Optimal Search** | Slow | High | Perfect | No |
| **Decay/Forget** | Medium | Low | Good | Yes ✓ |
| **Perfect Recall** | Fast | High | Perfect | No |

Alissa prioritizes "human-like" over "perfect" for more interesting behavior.

## Testing

Run indexing tests to verify behavior:

```bash
dotnet run --project tests
```

Tests verify:
- Lazy rebuild on access ✓
- Relevance decay works ✓
- Forgetting threshold enforced ✓
- Heuristic scoring returns results ✓
