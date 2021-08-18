[![.NET Core](https://github.com/trakx/coingecko-api-client/actions/workflows/dotnet-core.yml/badge.svg?branch=dev)](https://github.com/trakx/coingecko-api-client/actions/workflows/dotnet-core.yml) 
[![Codacy Badge](https://app.codacy.com/project/badge/Grade/28255efa057047a0b0c82b81c6ca386e)](https://www.codacy.com/gh/trakx/coingecko-api-client/dashboard?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=trakx/coingecko-api-client&amp;utm_campaign=Badge_Grade) 
[![Codacy Badge](https://app.codacy.com/project/badge/Coverage/28255efa057047a0b0c82b81c6ca386e)](https://www.codacy.com/gh/trakx/coingecko-api-client/dashboard?utm_source=github.com&utm_medium=referral&utm_content=trakx/coingecko-api-client&utm_campaign=Badge_Coverage)

# coingecko-api-client
C# implementation of a CoinGecko API client

## How to regenerate C# API clients

* If you work with external API, you probably need to update Change OpenAPI definition added to the project. It's usually openApi3.yaml file.
* Do right click on the project and select Edit Project File. In the file change value of `GenerateApiClient` property to true.
* Rebuild the project. `NSwag` target will be executed as post action.
* The last thing to be done is to run integration test `OpenApiGeneratedCodeModifier` that will rewrite auto generated C# classes to use C# 9 features like records and init keyword.
