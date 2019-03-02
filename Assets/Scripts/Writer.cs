using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using WebSocketSharp;
using SimpleJSON;
using System.Reflection;
using System.Collections;
using static Apollo.Utils;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using ErrorEventArgs = System.IO.ErrorEventArgs;

namespace Comm
{
    public interface Writer<T>
    {
        void Publish(T message, Action completed = null);
    }
}