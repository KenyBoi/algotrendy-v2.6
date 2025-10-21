#!/usr/bin/env dotnet-script
#r "nuget: KrakenExchange.Net, 6.9.0"

using Kraken.Net;
using Kraken.Net.Clients;
using System.Reflection;

var client = new KrakenRestClient();

// Test 1: Check GetBalancesAsync return type
Console.WriteLine("=== Test 1: GetBalancesAsync Return Type ===");
var balanceMethod = typeof(Kraken.Net.Interfaces.Clients.SpotApi.IKrakenRestClientSpotApiAccount)
    .GetMethod("GetBalancesAsync");
if (balanceMethod != null)
{
    var returnType = balanceMethod.ReturnType;
    Console.WriteLine($"Return Type: {returnType}");

    // Get the Task<T> inner type
    if (returnType.IsGenericType)
    {
        var innerType = returnType.GetGenericArguments()[0];
        Console.WriteLine($"Inner Type: {innerType}");

        // Get the WebCallResult<T> inner type
        if (innerType.IsGenericType)
        {
            var dataType = innerType.GetGenericArguments()[0];
            Console.WriteLine($"Data Type: {dataType}");

            // Check if it's a dictionary
            if (dataType.IsGenericType && dataType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                var keyType = dataType.GetGenericArguments()[0];
                var valueType = dataType.GetGenericArguments()[1];
                Console.WriteLine($"Dictionary Key Type: {keyType}");
                Console.WriteLine($"Dictionary Value Type: {valueType}");

                // Check properties of value type
                Console.WriteLine($"\nProperties of {valueType.Name}:");
                foreach (var prop in valueType.GetProperties())
                {
                    Console.WriteLine($"  - {prop.Name}: {prop.PropertyType}");
                }
            }
        }
    }
}

// Test 2: Check GetOpenOrdersAsync return type
Console.WriteLine("\n=== Test 2: GetOpenOrdersAsync Return Type ===");
var openOrdersMethod = typeof(Kraken.Net.Interfaces.Clients.SpotApi.IKrakenRestClientSpotApiTrading)
    .GetMethod("GetOpenOrdersAsync");
if (openOrdersMethod != null)
{
    var returnType = openOrdersMethod.ReturnType;
    Console.WriteLine($"Return Type: {returnType}");

    // Get the Task<T> inner type
    if (returnType.IsGenericType)
    {
        var innerType = returnType.GetGenericArguments()[0];
        Console.WriteLine($"Inner Type: {innerType}");

        // Get the WebCallResult<T> inner type
        if (innerType.IsGenericType)
        {
            var dataType = innerType.GetGenericArguments()[0];
            Console.WriteLine($"Data Type: {dataType}");

            // Check properties
            Console.WriteLine($"\nProperties of {dataType.Name}:");
            foreach (var prop in dataType.GetProperties())
            {
                Console.WriteLine($"  - {prop.Name}: {prop.PropertyType}");
            }
        }
    }
}

// Test 3: Check GetClosedOrdersAsync return type
Console.WriteLine("\n=== Test 3: GetClosedOrdersAsync Return Type ===");
var closedOrdersMethod = typeof(Kraken.Net.Interfaces.Clients.SpotApi.IKrakenRestClientSpotApiTrading)
    .GetMethod("GetClosedOrdersAsync");
if (closedOrdersMethod != null)
{
    var returnType = closedOrdersMethod.ReturnType;
    Console.WriteLine($"Return Type: {returnType}");

    // Get the Task<T> inner type
    if (returnType.IsGenericType)
    {
        var innerType = returnType.GetGenericArguments()[0];
        Console.WriteLine($"Inner Type: {innerType}");

        // Get the WebCallResult<T> inner type
        if (innerType.IsGenericType)
        {
            var dataType = innerType.GetGenericArguments()[0];
            Console.WriteLine($"Data Type: {dataType}");

            // Check properties
            Console.WriteLine($"\nProperties of {dataType.Name}:");
            foreach (var prop in dataType.GetProperties())
            {
                Console.WriteLine($"  - {prop.Name}: {prop.PropertyType}");
            }
        }
    }
}

// Test 4: Check GetTickerAsync return type
Console.WriteLine("\n=== Test 4: GetTickerAsync Return Type ===");
var tickerMethod = typeof(Kraken.Net.Interfaces.Clients.SpotApi.IKrakenRestClientSpotApiExchangeData)
    .GetMethod("GetTickerAsync");
if (tickerMethod != null)
{
    var returnType = tickerMethod.ReturnType;
    Console.WriteLine($"Return Type: {returnType}");

    // Get the Task<T> inner type
    if (returnType.IsGenericType)
    {
        var innerType = returnType.GetGenericArguments()[0];
        Console.WriteLine($"Inner Type: {innerType}");

        // Get the WebCallResult<T> inner type
        if (innerType.IsGenericType)
        {
            var dataType = innerType.GetGenericArguments()[0];
            Console.WriteLine($"Data Type: {dataType}");

            // Check properties
            Console.WriteLine($"\nProperties of {dataType.Name}:");
            foreach (var prop in dataType.GetProperties())
            {
                Console.WriteLine($"  - {prop.Name}: {prop.PropertyType}");
            }
        }
    }
}
