![GL-RTS_banner](https://forum.gameloop.it/assets/files/2019-02-22/1550838295-678699-gl-rts-banner.png)

# GameLoop.Networking
A UDP networking library for games made in GameLoop! It offers multiple abstraction levels, starting from low-level simple UDP wrapper up to higher-level ones.

## Usage of low-level UDP wrapper

```csharp
// Spin up a socket listener. It listens for datagrams from any network address, on the specified port.
var peer = new NetworkSocket(memoryPool);
peer.Bind(new IPEndPoint(IPAddress.Any, port));
```

```csharp
// Send data. It sends data to the specified network address and port.
peer.SendTo(new IPEndPoint(IPAddress.Loopback, port));
```

```csharp
// Polling for data. It fetches a message from the receiving queue, if any. If nothing has been received, it just returns "false".
while(peer.Poll(out NetworkArrivedData message))
{
	var addressFrom = message.EndPoint;
	var receivedData = message.Data

	// Execute your logic on received data.
}
```

## IMemoryPool and IMemoryAllocator

The library does not allocates memory directly. Instead it delegates this responsability to two interfaces: IMemoryPool and IMemoryAllocator.
By implementing these interfaces, you can manipulate how memory is allocated and managed in your application.
The library contains a trivial allocator and a trivial pool you can use to prototype quickly.

```csharp
var memoryAllocator = new SimpleManagedAllocator();
var memoryPool = new SimpleMemoryPool(memoryAllocator);
```