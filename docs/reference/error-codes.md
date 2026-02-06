# Error Codes Reference

## Error Code Format

`PCF-{CATEGORY}-{NUMBER}`

- **PCF**: Prefix for all PCF Builder errors
- **CATEGORY**: Error category (2-3 letters)
- **NUMBER**: Sequential number (3 digits)

## Categories

- **INT**: Intent Interpretation
- **CAP**: Capability Matching
- **SPEC**: Specification Generation
- **VAL**: Validation
- **GEN**: Code Generation
- **PKG**: Packaging
- **LLM**: LLM Service
- **SYS**: System Errors

---

## Intent Interpretation Errors (INT)

### PCF-INT-001: Invalid User Input
**Message**: User input is empty or invalid  
**Cause**: Empty string or malformed input  
**Solution**: Provide a valid component description  
**Example**: `""`

### PCF-INT-002: Ambiguous Intent
**Message**: Cannot determine component type from input  
**Cause**: Input is too vague  
**Solution**: Provide more specific details about the component  
**Example**: `"make something"`

### PCF-INT-003: Conflicting Requirements
**Message**: Input contains contradictory requirements  
**Cause**: Conflicting modifiers (e.g., "read-only editable")  
**Solution**: Clarify whether component should be read-only or editable  
**Example**: `"read-only editable rating"`

### PCF-INT-004: Schema Validation Failed
**Message**: Generated intent does not conform to schema  
**Cause**: LLM output invalid  
**Solution**: Retry or contact support  
**Internal**: Check LLM response parsing

---

## Capability Matching Errors (CAP)

### PCF-CAP-001: No Matching Capability
**Message**: No capability matches the requested component type  
**Cause**: Requested component type not supported  
**Solution**: Choose from available capabilities  
**Available**: star-rating

### PCF-CAP-002: Unsupported Feature
**Message**: Requested feature is not supported by capability  
**Cause**: Feature not in capability's `supportedFeatures`  
**Solution**: Remove unsupported feature or use alternative  
**Example**: Requesting video playback in star-rating

### PCF-CAP-003: Forbidden Behavior
**Message**: Requested behavior is explicitly forbidden  
**Cause**: Behavior in capability's `forbidden` list  
**Solution**: Use suggested alternative  
**Example**: External API calls, CDN dependencies

### PCF-CAP-004: Capability Load Failed
**Message**: Cannot load capability definition  
**Cause**: File not found or corrupted  
**Solution**: Contact support  
**Internal**: Check `ai-brain/capabilities/` directory

---

## Specification Generation Errors (SPEC)

### PCF-SPEC-001: Invalid Component Name
**Message**: Component name does not meet PCF requirements  
**Cause**: Name violates PCF naming rules  
**Solution**: Use PascalCase, alphanumeric only  
**Example**: `"123-rating"` → `"Rating123"`

### PCF-SPEC-002: Invalid Property Data Type
**Message**: Property data type is not a valid PCF type  
**Cause**: Unsupported data type  
**Solution**: Use supported PCF data types  
**Valid Types**: SingleLine.Text, Whole.None, Decimal, etc.

### PCF-SPEC-003: Missing Bound Property
**Message**: Component must have at least one bound property  
**Cause**: No property with `usage: "bound"`  
**Solution**: Add a bound property for data binding  
**Rule**: PCF_BINDING_001

### PCF-SPEC-004: Spec Schema Validation Failed
**Message**: Generated spec does not conform to schema  
**Cause**: LLM output invalid  
**Solution**: Retry or contact support  
**Internal**: Check component-spec.schema.json

---

## Validation Errors (VAL)

### PCF-VAL-001: Core Rule Violation
**Message**: Component violates PCF core compliance rule  
**Cause**: Error-level rule violation  
**Solution**: See specific rule message  
**Rules**: PCF_NAMING_001, PCF_BINDING_001, etc.

### PCF-VAL-002: Performance Rule Violation
**Message**: Component may have performance issues  
**Cause**: Warning-level performance rule  
**Solution**: Simplify component or accept warning  
**Rules**: PCF_PERF_001, PCF_PERF_002, etc.

### PCF-VAL-003: Accessibility Rule Violation
**Message**: Component does not meet accessibility standards  
**Cause**: Error-level accessibility rule  
**Solution**: Add required accessibility features  
**Rules**: PCF_A11Y_001, PCF_A11Y_003, etc.

### PCF-VAL-004: Security Rule Violation
**Message**: Component has security concerns  
**Cause**: Security rule violation  
**Solution**: Remove unsafe code patterns  
**Rules**: PCF_SECURITY_001, PCF_SECURITY_002

### PCF-VAL-005: Cross-Reference Failed
**Message**: Spec does not match capability constraints  
**Cause**: Spec violates capability limits  
**Solution**: Adjust spec to match capability  
**Example**: Requesting 15 stars when max is 10

---

## Code Generation Errors (GEN)

### PCF-GEN-001: Template Not Found
**Message**: Code template file not found  
**Cause**: Missing template file  
**Solution**: Contact support  
**Internal**: Check templates directory

### PCF-GEN-002: Template Rendering Failed
**Message**: Cannot render template with provided data  
**Cause**: Template syntax error or missing data  
**Solution**: Contact support  
**Internal**: Check template syntax

### PCF-GEN-003: Linting Failed
**Message**: Generated code does not pass linting  
**Cause**: Template produces invalid TypeScript  
**Solution**: Contact support  
**Internal**: Fix template

---

## Packaging Errors (PKG)

### PCF-PKG-001: Package Structure Invalid
**Message**: Generated package does not match PCF structure  
**Cause**: Missing required files  
**Solution**: Contact support  
**Internal**: Check packager logic

### PCF-PKG-002: ZIP Creation Failed
**Message**: Cannot create ZIP file  
**Cause**: File system error  
**Solution**: Retry or contact support  
**Internal**: Check disk space

### PCF-PKG-003: Package Validation Failed
**Message**: Package does not pass validation  
**Cause**: Invalid package structure  
**Solution**: Contact support  
**Internal**: Check package contents

---

## LLM Service Errors (LLM)

### PCF-LLM-001: API Call Failed
**Message**: LLM API call failed  
**Cause**: Network error, rate limit, or API error  
**Solution**: Retry or check API status  
**Retry**: Automatic with exponential backoff

### PCF-LLM-002: Invalid Response Format
**Message**: LLM response is not valid JSON  
**Cause**: LLM returned malformed JSON  
**Solution**: Retry  
**Retry**: Automatic up to max retries

### PCF-LLM-003: Response Validation Failed
**Message**: LLM response does not match expected schema  
**Cause**: LLM output invalid  
**Solution**: Retry  
**Retry**: Automatic up to max retries

### PCF-LLM-004: Timeout
**Message**: LLM request timed out  
**Cause**: Request exceeded timeout limit  
**Solution**: Retry or increase timeout  
**Default Timeout**: 30 seconds

### PCF-LLM-005: Rate Limit Exceeded
**Message**: LLM API rate limit exceeded  
**Cause**: Too many requests  
**Solution**: Wait and retry  
**Retry**: Automatic with backoff

---

## System Errors (SYS)

### PCF-SYS-001: Brain File Not Found
**Message**: Required AI Brain file not found  
**Cause**: Missing brain file  
**Solution**: Contact support  
**Internal**: Check ai-brain directory

### PCF-SYS-002: Brain File Parse Error
**Message**: Cannot parse AI Brain file  
**Cause**: Corrupted or invalid JSON  
**Solution**: Contact support  
**Internal**: Validate brain files

### PCF-SYS-003: Configuration Error
**Message**: Invalid system configuration  
**Cause**: Missing or invalid config  
**Solution**: Check configuration  
**Required**: namespace, llmProvider, llmModel

### PCF-SYS-004: Internal Server Error
**Message**: Unexpected internal error  
**Cause**: Unhandled exception  
**Solution**: Contact support  
**Internal**: Check logs

---

## Error Response Format

```json
{
  "status": "error",
  "error": {
    "code": "PCF-CAP-001",
    "stage": "capability-matching",
    "message": "No capability matches the requested component type",
    "userMessage": "Cannot create a video player component. Video player components are not currently supported.",
    "suggestion": "Choose from available component types",
    "alternatives": ["star-rating"],
    "details": {
      "requestedType": "video-player",
      "availableCapabilities": ["star-rating"]
    }
  }
}
```

## Handling Errors

### Client-Side
1. Display `userMessage` to user
2. Show `suggestion` if available
3. Offer `alternatives` if available
4. Log full error for debugging

### Server-Side
1. Log error with full details
2. Add to audit trail
3. Return formatted error response
4. Increment error metrics

## Retry Strategy

| Error Code | Retry | Max Retries | Backoff |
|------------|-------|-------------|---------|
| PCF-LLM-001 | Yes | 3 | Exponential |
| PCF-LLM-002 | Yes | 3 | Exponential |
| PCF-LLM-003 | Yes | 2 | Linear |
| PCF-LLM-004 | Yes | 1 | None |
| PCF-LLM-005 | Yes | 5 | Exponential |
| All others | No | 0 | N/A |
