using System;
using UnityEngine;

[Serializable]
public sealed class ShopProduct
{
    [SerializeField] private CollectibleDefinition collectible;
    [SerializeField, Min(0)] private int price;
    [SerializeField, Min(1)] private int stock = 1;

    public CollectibleDefinition Collectible => collectible;
    public int Price => price;
    public int Stock => stock;
}

[CreateAssetMenu(
    fileName = "ShopInventory",
    menuName = "Zero/Shop Inventory"
)]
public sealed class ShopInventoryDefinition : ScriptableObject
{
    [SerializeField] private string shopId;
    [SerializeField] private ShopProduct[] products;

    public string ShopId => shopId;
    public ShopProduct[] Products => products;
}
