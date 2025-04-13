# Problem Statement.

We are using /resources/news-database/news.db SQlite database to configure and store news items. We have defined news sources that tell us where to look, and our news function go out and check on a schedule. If anything new and newsworthy is found, we store it in the news items table.

So far, we've only done one function the nuget package one. I want to simplify the approach and make it more generic so that it doesn't become complex for every type of news category we are scanning.

Example - for nuget packages we want to only check if an update has been made to a certain nuget package. First time we run this it will detect lets say version 9.3 - we store that in the news item table and move on. Then on next run the latest is version is still 9.3 and we have nothing new to report. Lets say then 9.4 comes out and we want to of course store that as another row in the news items table. We basically want to capture basic information and we don't want to make something a news item if nothing has changed. So the whole thing about the Models/NuGetPackage is too complex - we need to stick with the two models defined in HemSoft.News.Data and have the IChatClient help us determine if something is new or not.

One more thing, if we search for lets say "A.B.C" we want to only check that one nuget package, we don't want results for nuget package "A.B.C.1" and "A.B.C.2" etc.
