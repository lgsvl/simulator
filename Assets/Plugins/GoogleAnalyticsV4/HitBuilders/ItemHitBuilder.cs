/*
  Copyright 2014 Google Inc. All rights reserved.

  Licensed under the Apache License, Version 2.0 (the "License");
  you may not use this file except in compliance with the License.
  You may obtain a copy of the License at

      http://www.apache.org/licenses/LICENSE-2.0

  Unless required by applicable law or agreed to in writing, software
  distributed under the License is distributed on an "AS IS" BASIS,
  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
  See the License for the specific language governing permissions and
  limitations under the License.
*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class ItemHitBuilder : HitBuilder<ItemHitBuilder> {

  private string transactionID = "";
  private string name = "";
  private string SKU = "";
  private double price;
  private string category = "";
  private long quantity;
  private string currencyCode = "";

  public string GetTransactionID() {
    return transactionID;
  }

  public ItemHitBuilder SetTransactionID(string transactionID) {
    if(transactionID != null){
      this.transactionID = transactionID;
    }
    return this;
  }

  public string GetName() {
    return name;
  }

  public ItemHitBuilder SetName(string name) {
    if(name != null){
      this.name = name;
    }
    return this;
  }

  public string GetSKU() {
    return SKU;
  }

  public ItemHitBuilder SetSKU(string SKU) {
    if(SKU != null){
      this.SKU = SKU;
    }
    return this;
  }

  public double GetPrice() {
    return price;
  }

  public ItemHitBuilder SetPrice(double price) {
    this.price = price;
    return this;
  }

  public string GetCategory() {
    return category;
  }

  public ItemHitBuilder SetCategory(string category) {
    if(category != null){
      this.category = category;
    }
    return this;
  }

  public long GetQuantity() {
    return quantity;
  }

  public ItemHitBuilder SetQuantity(long quantity) {
    this.quantity = quantity;
    return this;
  }

  public string GetCurrencyCode() {
    return currencyCode;
  }

  public ItemHitBuilder SetCurrencyCode(string currencyCode) {
    if(currencyCode != null){
      this.currencyCode = currencyCode;
    }
    return this;
  }

  public override ItemHitBuilder GetThis(){
    return this;
  }

  public override ItemHitBuilder Validate(){
    if(String.IsNullOrEmpty(transactionID)){
      Debug.LogWarning("No transaction ID provided - Item hit cannot be sent.");
      return null;
    }
    if(String.IsNullOrEmpty(name)){
      Debug.LogWarning("No name provided - Item hit cannot be sent.");
      return null;
    }
    if(String.IsNullOrEmpty(SKU)){
      Debug.LogWarning("No SKU provided - Item hit cannot be sent.");
      return null;
    }
    if(price == 0.0D){
      Debug.Log("Price in item hit is 0.");
    }
    if(quantity == 0L){
      Debug.Log("Quantity in item hit is 0.");
    }
    return this;
  }
}
