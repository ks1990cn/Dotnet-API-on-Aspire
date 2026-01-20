This is a sample web api application built to run on docker and kubernetes using .Net Aspire. This I have created as per Assessment shared by BambooCards.

**Demo**

![Bamboo](https://github.com/user-attachments/assets/fd5499a9-a79c-45b6-a379-e3033bef9e9a)

**Required tools**
- Docker
- VS 2026
- .Net 10

**Steps to start application**
- Open .sln in vs 2026.
- Run AppHost as startup project.

**Open source tools integrated are below**
- Keycloak for JWT token and user
- Redis for caching

**Steps to run on kubernetes**
This project is scalabale to any extent just deploy on kubernetes using helm and increase number of pods as per your wish.
https://www.youtube.com/watch?v=z2sw3c_h-qc

**API Reference**

Curl to fetch token:
  
<pre>
curl --location 'http://localhost:8080/realms/bamboo-realm/protocol/openid-connect/token' \
--header 'Content-Type: application/x-www-form-urlencoded' \
--data-urlencode 'client_id=bamboo-api' \
--data-urlencode 'client_secret=0wrv4QPKgWHHBIq3TofhYesEYMGqj9GH' \
--data-urlencode 'grant_type=password' \
--data-urlencode 'username=testuser' \
--data-urlencode 'password=1234'
</pre>

1. Get Currency Exchange
Returns the exchange rate between two specific currencies. This endpoint utilizes Redis caching to ensure high performance and reduce latency for frequent queries.

    
   Authentication/Authorization: Required (Bearer Token / bamboo_user Role)
    
    Cache TTL: 60 Minutes
   
<pre>
curl --location 'https://localhost:7051/GetCurrencyExchange?fromCurrency=USD&toCurrency=EUR&amount=10' \
--header 'Authorization: Bearer '
</pre>



2. Get Latest Exchange Rates
Retrieves the most recent exchange rates for a base currency against all supported currencies.

[!IMPORTANT] Rate Limiting: This endpoint is limited to 5 requests every 10 minutes per API key. Exceeding this limit will result in a 429 Too Many Requests error.

  
  Authentication/Authorization: Required (Bearer Token / bamboo_user Role)
  
<pre>
curl --location 'https://localhost:7051/GetLatestExchangeRates?baseCurrency=EUR' \
--header 'Authorization: Bearer '
</pre>

3. Get Historical Data
Fetches historical exchange rate data for a specific date in the past.

    
    Authentication/Authorization: Required (Bearer Token / bamboo_user Role)
   

<pre>
curl --location 'https://localhost:7051/GetHistoryBySymbolAndDateRange?fromDate=2026-01-04&symbol=INR' \
--header 'Authorization: Bearer '
</pre>  


**Possible future enhancements**
- More integration/unit tests can be added.
- Client Secret is as of now in appsettings, it can be secured in vault.
- Ci/Cd with helm can be done for deployment.
- Swagger can be added.
