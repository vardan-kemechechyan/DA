using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Purchasing;
using UnityEngine.PlayerLoop;
using System.Linq;

#if GAMEANALYTICS
using GameAnalyticsSDK;
#endif

public class IAP : MonoBehaviour, IStoreListener
{
    private static IStoreController storeController;
    private static IExtensionProvider storeExtensionProvider;

    private static SubscriptionManager subscriptionManager;

    [SerializeField] Item[] items;
    public Item[] Items => items;

    public event OnSuccessPurchase OnPurchase;
    public delegate void OnSuccessPurchase(Item item);
    public event OnRestorePurchases OnRestore;
    public delegate void OnRestorePurchases();

    public bool IsNoAdsPurchased() 
    {
        return IsPurchased(GetItem("no_ads"));
    }

    public Item GetItem(string id) 
    {
        return items.FirstOrDefault(x => x.id.Equals(id));
    }

    private Item GetItemWithProduct(Product product) 
    {
        return items.FirstOrDefault(x => x.product.Equals(product));
    }

    private int GetAmountFromLocalizedPrice(decimal localizedPrice) 
    {
        return Mathf.FloorToInt((float)localizedPrice * 100);
    }

    public bool IsInitialized { get; private set; }

    public bool IsPurchased(string storeId)
    {
        return IsPurchased(GetItem(storeId));
    }

    public bool IsPurchased(Item item)
    {
        if (IsInitialized)
        {
            if (item.product != null && item.product.hasReceipt) return true;
            else return false;
        }
        else 
        {
            return false;
        }
    }

    public void Initialize()
    {
        if (storeController == null) 
        {
            if (IsInitialized)
            {
                return;
            }

            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            foreach (Item item in items)
                builder.AddProduct(item.id, item.type);

            UnityPurchasing.Initialize(this, builder);
        }
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        storeController = controller;
        storeExtensionProvider = extensions;

        foreach (var item in Items)
            item.product = storeController.products.WithID(item.id);

        IsInitialized = true;
    }

    public void Purchase(Item item)
    {
        if (IsInitialized)
        {
            if (item.product != null && item.product.availableToPurchase)
            {
                Debug.Log($"IAP Purchasing {item.product.definition.id} ...");
                storeController.InitiatePurchase(item.product);

                int amount = Mathf.FloorToInt((float)item.product.metadata.localizedPrice * 100);

                Debug.Log($"IAP Price: {item.product.metadata.localizedPrice} amount: {amount}");

#if GAMEANALYTICS
                GameAnalytics.NewBusinessEvent(item.product.metadata.isoCurrencyCode, amount, item.product.definition.type.ToString(), item.id, "shop");
#endif
            }
            else
            {
                Debug.LogWarning($"IAP {item.type} {item.id} not found!");
            }
        }
        else
        {
            Debug.LogWarning($"IAP not initialized!");
        }
    }

    public void Restore() 
    {
        if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.OSXPlayer)
        {
            storeExtensionProvider.GetExtension<IAppleExtensions>().RestoreTransactions((result) =>
            {
                Debug.Log($"IAP Purchases restored {result}");
                OnRestore?.Invoke();
            });
        }
        else
        {
            Debug.Log($"IAP Not supported on {Application.platform}");
            OnRestore?.Invoke();
        }
    }

    public void OnPurchaseComplete(Product product)
    {
        Debug.Log(product.metadata);
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.LogWarning("IAP failed to initialize: " + error);
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        OnPurchase?.Invoke(GetItemWithProduct(args.purchasedProduct));

#if GAMEANALYTICS
        GameAnalytics.NewBusinessEvent(args.purchasedProduct.metadata.isoCurrencyCode, GetAmountFromLocalizedPrice(args.purchasedProduct.metadata.localizedPrice),
            args.purchasedProduct.definition.type.ToString(), args.purchasedProduct.definition.id, "shop");
#endif

        // Report revenue
        var product = args.purchasedProduct;

        string currency = product.metadata.isoCurrencyCode;
        decimal price = product.metadata.localizedPrice;

        // Creating the instance of the YandexAppMetricaRevenue class.
        YandexAppMetricaRevenue revenue = new YandexAppMetricaRevenue(price, currency);
        if (product.receipt != null)
        {
            // Creating the instance of the YandexAppMetricaReceipt class.
            YandexAppMetricaReceipt yaReceipt = new YandexAppMetricaReceipt();
            Receipt receipt = JsonUtility.FromJson<Receipt>(product.receipt);
#if UNITY_ANDROID
            PayloadAndroid payloadAndroid = JsonUtility.FromJson<PayloadAndroid>(receipt.Payload);
            yaReceipt.Signature = payloadAndroid.Signature;
            yaReceipt.Data = payloadAndroid.Json;
#elif UNITY_IPHONE
            yaReceipt.TransactionID = receipt.TransactionID;
            yaReceipt.Data = receipt.Payload;
#endif
            revenue.Receipt = yaReceipt;
        }

        // Sending data to the AppMetrica server.
        AppMetrica.Instance.ReportRevenue(revenue);

        return PurchaseProcessingResult.Complete;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
    {
        var item = GetItemWithProduct(product);
        Debug.Log($"IAP Failed to purchase {item.type} {item.id}! storeSpecificIdt: {product.definition.storeSpecificId} reason: {reason}");
    }

    [Serializable]
    public class Item
    {
        public string id;
        public UnityEngine.Purchasing.ProductType type;
        public UnityEngine.Purchasing.Product product;

        public string GetPrice() 
        {
            if (product != null && product.metadata != null && !string.IsNullOrEmpty(product.metadata.isoCurrencyCode))
                return $"{product.metadata.localizedPrice} {product.metadata.isoCurrencyCode}";
            else
            {
                Debug.Log($"IAP Cannot get price for {id}!");
                return "";
            }
        }
    }

    [Serializable]
    public struct Receipt
    {
        public string Store;
        public string TransactionID;
        public string Payload;
    }

    [Serializable]
    public struct PayloadAndroid
    {
        public string Json;
        public string Signature;
    }
}
