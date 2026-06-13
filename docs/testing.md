# Testing Guide

## Running Tests

### Run All Tests

```bash
dotnet run --project tests
```

### Expected Output

```
╔════════════════════════════════════════╗
║  ALISSA ARCHITECTURE REFACTOR TESTS    ║
╚════════════════════════════════════════╝

=== MEMORY PIPELINE TESTS ===

[TEST] Memory Pipeline Flow
  ✓ Summary generated: 245 chars
  ✓ Topics identified: 4
  ✓ Extraction result: True

[TEST] Session Cache Purity
  ✓ Session cache contains only conversation messages

[TEST] Memory Extraction Flexibility
  ✓ Extraction returned data: Profile: 0, Facts: 0, Skills: 0, Learnings: 0

=== INDEXING TESTS ===

[TEST] Lazy Rebuild on Access
  ✓ Index rebuilds on access (lazy evaluation)

[TEST] Relevance Decay & Forgetting
  ✓ Relevance decay applied to old memories

[TEST] Heuristic Scoring
  ✓ Heuristic search found 1 relevant memories

=== PROMPT BUILDER TESTS ===

[TEST] Prompt Section Injection
  ✓ All personality sections injected

[TEST] Token Budgeting
  ✓ Token budget respected (prompt size reasonable)

[TEST] Priority Order in Trimming
  ✓ Priority order configured and applied

=== CONFIGURATION TESTS ===

[TEST] Configuration Loading
  ✓ All configuration sections loaded
  ✓ New configuration rules loaded

[TEST] PromptRulesModel Deserialization
  ✓ PromptRulesModel has valid defaults

[TEST] PersonalityRulesModel Deserialization
  ✓ PersonalityRulesModel configured for catgirl personality

[TEST] IndexingRulesModel Deserialization
  ✓ IndexingRulesModel configured for 'dumb' indexing

╔════════════════════════════════════════╗
║  ✓ ALL TESTS COMPLETED SUCCESSFULLY   ║
╚════════════════════════════════════════╝
```

## Test Coverage

### MemoryPipelineTests

Tests the complete flow from conversation to memory storage:

1. **TestMemoryPipelineFlow**: 
   - Verifies summary generation works
   - Checks topic extraction
   - Confirms memory extraction produces results

2. **TestSessionCachePurity**:
   - Confirms session cache contains only messages
   - Verifies no summaries pollute cache
   - Ensures proper isolation

3. **TestMemoryExtractionIsFlexible**:
   - Tests flexible JSON parsing
   - Verifies graceful failure on incomplete data

### IndexingTests

Tests the lazy, "dumb" indexing system:

1. **TestLazyRebuild**:
   - Confirms index rebuilds on access (not cached)
   - Verifies lazy evaluation behavior

2. **TestForgetfulness**:
   - Tests relevance decay over time
   - Verifies old memories lose relevance
   - Checks forgetting threshold

3. **TestHeuristicScoring**:
   - Tests heuristic search function
   - Verifies queries find relevant memories

### PromptBuilderTests

Tests prompt construction with token budgeting:

1. **TestPromptSectionInjection**:
   - Confirms all personality sections loaded
   - Verifies user profile injection
   - Checks proper section ordering

2. **TestTokenBudgeting**:
   - Tests token limit enforcement
   - Verifies prompt stays within budget
   - Checks section trimming

3. **TestPriorityOrderRespected**:
   - Confirms high-priority sections preserved
   - Verifies low-priority sections trimmed first

### ConfigurationTests

Tests configuration loading and deserialization:

1. **TestConfigLoading**:
   - Verifies all config files load
   - Checks required sections
   - Confirms new config sections available

2. **TestPromptRulesModel**:
   - Verifies defaults are valid
   - Checks token budgets

3. **TestPersonalityRulesModel**:
   - Confirms catgirl traits enabled
   - Verifies unfiltered mode set
   - Checks silliness level

4. **TestIndexingRulesModel**:
   - Confirms lazy rebuild enabled
   - Verifies forgetfulness enabled
   - Checks heuristic scoring enabled

## Test Infrastructure

### MockChatClient

Mock implementation of IChatClient for testing without Ollama:

```csharp
public class MockChatClient : IChatClient
{
	public async IAsyncEnumerable<string> StreamAsync(string systemPrompt, string userInput)
	{
		await Task.Delay(10);
		yield return "This is a test response.";
	}
}
```

### Temporary Directories

Each test uses isolated temp directory:
- Prevents interference between tests
- Cleans up after execution
- Allows safe file system operations

### Test Patterns

All tests follow pattern:
1. Create isolated temp directory
2. Create necessary config/data
3. Run test logic
4. Assert results
5. Cleanup temp directory

## Verification Checklist

Before declaring refactor complete:

- [ ] All tests pass
- [ ] No compilation warnings
- [ ] Session cache stores only messages (no summaries)
- [ ] Memory extraction produces structured data
- [ ] Lazy indexing rebuilds on access
- [ ] Relevance decay works (old memories forgotten)
- [ ] Token budgeting enforced
- [ ] Configuration loads all files
- [ ] AlissaClient is only public API
- [ ] No direct OllamaClient calls from main
- [ ] Personality fields available in config
- [ ] Medium-term memory works when enabled

## Debugging Tests

### Enable Verbose Output

Modify test to print intermediate values:

```csharp
var index = indexBuilder.BuildIndex();
Console.WriteLine($"Index size: {index.Entries.Count}");
Console.WriteLine($"Total memories: {index.TotalMemories}");
foreach (var entry in index.Entries.Values.Take(3))
{
	Console.WriteLine($"  - {entry}: {entry.Relevance:F2}");
}
```

### Check Configuration

Verify config files exist and have valid JSON:

```bash
cat config/model.json | jq .
cat config/personality_rules.json | jq .
cat config/indexing_rules.json | jq .
```

### Test Individual Services

Create minimal test file:

```csharp
var memoryManager = new MemoryManager(testDir, config.Memory);
var promptBuilder = new PromptBuilder(testDir, memoryManager);
var summaryService = new SummaryGenerationService(chatClient, promptBuilder);

var summary = await summaryService.GenerateSummaryAsync("test text", 3);
Console.WriteLine($"Generated: {summary}");
```

## Extending Tests

To add new tests:

1. Create test class with static RunAllTests() method
2. Implement individual test methods
3. Call from tests/Program.cs
4. Follow existing patterns (temp dir, cleanup, assertions)

Example:

```csharp
public class MyNewTests
{
	public static void RunAllTests()
	{
		Console.WriteLine("\n=== MY NEW TESTS ===\n");
		TestFeatureX();
		TestFeatureY();
	}

	private static void TestFeatureX()
	{
		Console.WriteLine("[TEST] Feature X");
		// test logic
		Console.WriteLine(success ? "  ✓ Feature X works" : "  ✗ Feature X failed");
	}

	private static void TestFeatureY()
	{
		Console.WriteLine("[TEST] Feature Y");
		// test logic
	}
}
```

Then add to Program.cs:

```csharp
await MyNewTests.RunAllTests();
```
