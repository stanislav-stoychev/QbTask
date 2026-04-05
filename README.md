## How to run

Navigate to Backend folder. Run these commands:
* dotnet restore
* dotnet run

Open browser and navigate to http://localhost:5094/swagger/index.html

## Description and Design Choices
This project uses Vertical Slice Architecture (or at least attempts to) built on the FastEndpoints framework. For data access, I chose Dapper, as it is a convenient and easy-to-use "database-first" library.

Because of the possibility of intermittent failures, I have implemented Polly for a retry mechanism. Scrutor provides an easy way to inject dependencies in bulk. There is also structured logging in place, which can potentially be hooked to a sink (such as Elasticsearch). This could help re-evaluate the matching alias vectors and "junk" regexes based on real-world data.

## The Aggregator Logic

The general idea is that the Aggregator class can pick up all available country population data sources and aggregate the data according to business needs.
The system allows for flexibility when adding new data sources: Implement the IStatService interface and add the corresponding setting under StatAggregationConfiguration:StatSourcesPrecedence.
The precedence (or weight) of each data source is determined by its position in the StatSourcesPrecedence array.
Since country names can differ across data sources, there are three ways to determine a match:
* Direct Comparison: A standard string equality check.
* Alias Vector Search: Checking if a match exists between two entities from different data sources within a defined alias list.
* Junk Removal: A configuration setting containing specific expressions. If two country names are equal after these expressions are stripped, they are considered a match. Example: If the junk list contains ["democratic", "republic", "of"], the names "Congo" and "Democratic Republic of Congo" would result in a match.
