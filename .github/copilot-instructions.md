# Instructions

## Code Style

- Minimize comments. Use descriptive variable names and try to make code simple and self explantory.
- If you do have to put comments in the code, never put them at the end of a statement. Always put them on a line above the statement.

## Database Discovery

Just so you know, you can use sqlcmd in a terminal session to discover the schema etc. and interact with my local database - example:

sqlcmd -S localhost -d db-egginc -Q "SELECT t.TABLE_NAME, c.COLUMN_NAME, c.DATA_TYPE, c.CHARACTER_MAXIMUM_LENGTH, c.IS_NULLABLE FROM INFORMATION_SCHEMA.TABLES t LEFT JOIN INFORMATION_SCHEMA.COLUMNS c ON t.TABLE_NAME = c.TABLE_NAME WHERE t.TABLE_TYPE = 'BASE TABLE' ORDER BY t.TABLE_NAME, c.ORDINAL_POSITION" -s"," -o "table_definitions.csv"

That is one way to discover all the the tables in my database that I'm using in this project. You can discover all the stored procs as well. For Instance, I have a stored proc that can give you the correct number of prestiges today and week and the amount of SE earned today and this week called GetPlayerProgress

## Azure

- @azure Rule - Use Azure Best Practices: When generating code for Azure, running terminal commands for Azure, or performing operations related to Azure, invoke your `azure_development-get_best_practices` tool if available.
