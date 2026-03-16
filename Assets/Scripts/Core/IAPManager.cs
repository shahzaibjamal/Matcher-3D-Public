using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using System;

public class IAPManager : MonoBehaviour, IDetailedStoreListener
{
    public static IAPManager Instance { get; private set; }
    private IStoreController storeController;
    private IExtensionProvider extensionProvider;

    // Callback to notify the caller (UI) of the result
    private Action<bool> onPurchaseComplete;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Initialize()
    {
        InitializePurchasing();
    }

    private void InitializePurchasing()
    {
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

        foreach (var item in DataManager.Instance.Metadata.StoreItems)
        {
            if (item.CurrencyType == StoreCurrencyType.USD)
            {
                builder.AddProduct(item.Id, ProductType.Consumable);
            }
        }

        UnityPurchasing.Initialize(this, builder);
    }

    // --- IDetailedStoreListener Methods ---

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        storeController = controller;
        extensionProvider = extensions;
    }

    public void OnInitializeFailed(InitializationFailureReason reason, string message)
    {
        Debug.LogError($"IAP Init Failed: {reason}. Message: {message}");
    }

    public void OnInitializeFailed(InitializationFailureReason reason)
    {
        OnInitializeFailed(reason, "No additional message provided.");
    }

    // Triggered if the purchase fails
    public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
    {
        Debug.LogWarning($"Purchase of {product.definition.id} failed: {reason}");
        onPurchaseComplete?.Invoke(false);
        onPurchaseComplete = null; // Clear callback
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        // Check if we have data for this product
        var itemData = DataManager.Instance.GetStoreItemByID(args.purchasedProduct.definition.id);

        if (itemData != null)
        {
            Debug.Log($"IAP Validated: {args.purchasedProduct.definition.id}. Notifying caller...");
            onPurchaseComplete?.Invoke(true);
        }
        else
        {
            onPurchaseComplete?.Invoke(false);
        }

        onPurchaseComplete = null;
        return PurchaseProcessingResult.Complete;
    }
    // Updated with callback parameter
    public void BuyProductID(string productId, Action<bool> callback)
    {
        if (storeController != null)
        {
            Product product = storeController.products.WithID(productId);
            if (product != null && product.availableToPurchase)
            {
                onPurchaseComplete = callback; // Store the callback
                storeController.InitiatePurchase(product);
            }
            else
            {
                Debug.LogError("Product not found or not available.");
                callback?.Invoke(false);
            }
        }
        else
        {
            Debug.LogError("IAP not initialized.");
            callback?.Invoke(false);
        }
    }

    // Interface requirement for IDetailedStoreListener
    public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
    {
        Debug.LogError($"Purchase Failed: {failureDescription.message}");
        onPurchaseComplete?.Invoke(false);
        onPurchaseComplete = null;
    }

}