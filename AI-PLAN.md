# AI Integration Plan for HemSoft.News.Functions

## Overview

This document outlines the plan to replace the current `ParseNuGetPackages()` method in the HemSoft.News.Functions project with an AI-based solution. The current implementation uses regex patterns to extract package information, which may not be ideal for parsing complex content. We'll implement a more robust solution using a small LLM to help parse the content.

## Project Structure

We'll create two new projects:

1. **HemSoft.AI** - A reusable AI client library that can be used across different projects
2. **HemSoft.News.Tools** - A library for MCP (Model-Completion-Protocol) tools that can be used with the AI client

## Implementation Steps

### Phase 1: Create HemSoft.AI Project

1. Create a new class library project called `HemSoft.AI`
2. Implement a flexible ChatClient based on the reference project `/ref/ChatClient-POC`
3. Implement factory methods to customize the ChatClient with different system prompts and tooling
4. Add necessary NuGet packages:
   - Azure.AI.OpenAI
   - Microsoft.Extensions.AI
   - Microsoft.Extensions.Configuration
   - Microsoft.Extensions.DependencyInjection

### Phase 2: Create HemSoft.News.Tools Project

1. Create a new class library project called `HemSoft.News.Tools`
2. Implement MCP protocol handlers to connect tools to the ChatClient
3. Create specific tools for news content parsing
4. Add necessary NuGet packages:
   - Microsoft.Extensions.AI
   - Microsoft.Extensions.Logging.Abstractions

### Phase 3: Integrate with HemSoft.News.Functions

1. Add references to the new projects in HemSoft.News.Functions
2. Create a new service for AI-based content parsing
3. Replace the current `ParseNuGetPackages()` method with the new AI-based solution
4. Update dependency injection in Program.cs

### Phase 4: Testing and Refinement

1. Create unit tests for the new AI-based solution
2. Compare results with the current implementation
3. Refine the system prompts and tools as needed
4. Optimize performance and error handling

## Detailed Implementation

### HemSoft.AI Project

#### Core Components

1. **ChatClient Class**
   - Wrapper around Azure OpenAI client
   - Support for system prompts, user messages, and tool integration
   - Configurable model selection

2. **ChatClientFactory**
   - Factory methods to create customized ChatClient instances
   - Support for different system prompts and tool configurations

3. **AIToolBase Class**
   - Base class for implementing AI tools
   - Integration with Microsoft.Extensions.AI

#### Configuration

- Use .NET User Secrets for storing API keys and endpoints
- Support for configuration via appsettings.json or environment variables

### HemSoft.News.Tools Project

#### Core Components

1. **MCPToolHandler**
   - Implementation of the MCP protocol
   - Connection between tools and ChatClient

2. **NewsContentParser**
   - Specialized tool for parsing news content
   - Support for different content formats (NuGet, blogs, etc.)

3. **MCPServer (Optional)**
   - Server implementation for hosting MCP tools
   - Can be used as a source for news content

### Integration with HemSoft.News.Functions

1. **AIContentParsingService**
   - Service for parsing content using AI
   - Integration with the existing `ProcessScrapedContentAsync` method

2. **Updated NuGetMonitorFunction**
   - Replace the current `ParseNuGetPackages()` method
   - Use the new AI-based solution for content parsing

## Technical Considerations

### AI Model Selection

- Use Azure OpenAI's GPT models (gpt-4o-mini or similar)
- Consider model size vs. performance tradeoffs
- Implement fallback mechanisms for API failures

### System Prompts

- Create specialized prompts for parsing different types of content
- Include examples of expected input and output formats
- Optimize prompts for accuracy and efficiency

### Error Handling

- Implement robust error handling for AI API calls
- Provide fallback mechanisms when AI parsing fails
- Log detailed error information for debugging

### Performance Optimization

- Implement caching mechanisms for similar content
- Optimize token usage in prompts and responses
- Consider batching requests for multiple news items

## Future Enhancements

1. **Support for Additional Content Types**
   - Extend the AI parsing capabilities to handle more news source types
   - Create specialized prompts and tools for each content type

2. **Improved Content Extraction**
   - Extract more detailed information from news sources
   - Implement entity recognition for people, organizations, and technologies

3. **Content Summarization**
   - Use AI to generate summaries of news articles
   - Categorize news items based on content

4. **Integration with Other Systems**
   - Connect to additional news sources
   - Implement webhooks for real-time notifications

## Timeline

1. **Week 1**: Set up project structure and implement HemSoft.AI
2. **Week 2**: Implement HemSoft.News.Tools and integrate with HemSoft.News.Functions
3. **Week 3**: Testing, refinement, and documentation
4. **Week 4**: Deployment and monitoring

## Conclusion

This plan outlines a comprehensive approach to replacing the current `ParseNuGetPackages()` method with an AI-based solution. By leveraging Azure OpenAI and implementing a flexible architecture, we can create a more robust and accurate content parsing system that can be extended to handle various types of news content.
