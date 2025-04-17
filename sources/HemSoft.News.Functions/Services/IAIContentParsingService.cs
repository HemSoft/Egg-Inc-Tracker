namespace HemSoft.News.Functions.Services;

using System.Collections.Generic;
using System.Threading.Tasks;
using HemSoft.News.Data.Models;

/// <summary>
/// Interface for parsing content using AI
/// </summary>
public interface IAIContentParsingService
{
    /// <summary>
    /// Parses NuGet packages from content
    /// </summary>
    /// <param name="source">The news source</param>
    /// <param name="content">The content to parse</param>
    /// <returns>A list of news items</returns>
    Task<List<NewsItem>> ParseNuGetPackagesAsync(NewsSource source, string content);
}
