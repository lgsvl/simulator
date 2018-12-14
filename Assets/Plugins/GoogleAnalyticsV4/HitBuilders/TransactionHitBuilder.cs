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

public class TransactionHitBuilder : HitBuilder<TransactionHitBuilder> {

  private string transactionID = "";
  private string affiliation = "";
  private double revenue;
  private double tax;
  private double shipping;
  private string currencyCode = "";

  public string GetTransactionID() {
      return transactionID;
  }

  public TransactionHitBuilder SetTransactionID(string transactionID) {
    if(transactionID != null){
      this.transactionID = transactionID;
    }
    return this;
  }

  public string GetAffiliation() {
    return affiliation;
  }

  public TransactionHitBuilder SetAffiliation(string affiliation) {
    if(affiliation != null){
      this.affiliation = affiliation;
    }
    return this;
  }

  public double GetRevenue() {
    return revenue;
  }

  public TransactionHitBuilder SetRevenue(double revenue) {
    this.revenue = revenue;
    return this;
  }

  public double GetTax() {
    return tax;
  }

  public TransactionHitBuilder SetTax(double tax) {
    this.tax = tax;
    return this;
  }

  public double GetShipping() {
    return shipping;
  }

  public TransactionHitBuilder SetShipping(double shipping) {
    this.shipping = shipping;
    return this;
  }

  public string GetCurrencyCode() {
    return currencyCode;
  }

  public TransactionHitBuilder SetCurrencyCode(string currencyCode) {
    if(currencyCode != null){
      this.currencyCode = currencyCode;
    }
    return this;
  }

  public override TransactionHitBuilder GetThis(){
    return this;
  }

  public override TransactionHitBuilder Validate(){
    if(String.IsNullOrEmpty(transactionID)){
      Debug.LogWarning("No transaction ID provided - Transaction hit cannot be sent.");
      return null;
    }
    if(String.IsNullOrEmpty(affiliation)){
      Debug.LogWarning("No affiliation provided - Transaction hit cannot be sent.");
      return null;
    }
    if(revenue == 0){
      Debug.Log("Revenue in transaction hit is 0.");
    }
    if(tax == 0){
      Debug.Log("Tax in transaction hit is 0.");
    }
    if(shipping == 0){
      Debug.Log("Shipping in transaction hit is 0.");
    }
    return this;
  }
}
