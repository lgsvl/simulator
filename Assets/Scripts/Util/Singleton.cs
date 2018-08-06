/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */
 
 /*
 * Abstract Singleton for non-Monobehaviour classes
 */
public abstract class Singleton<T> where T : new()
{

    private static T _instance;

    public static T Instance
    {
        get
        {
            if(_instance == null)
                _instance = new T();

            return _instance;
        }
    }
}