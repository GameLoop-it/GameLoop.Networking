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

## Working with buffers

The library includes some convenient structures to work with buffers, to help the user to write/read to/from them.

```csharp
// Reading from a buffer.
byte[] buffer = message.Data;

var reader = default(NetworkReader);
reader.Initialize(ref buffer);

int myInt = reader.ReadInt();
long myLong = reader.ReadLong();
float myFloat = reader.ReadFloat();
string myString = reader.ReadString();
// etc
```

```csharp
// Writing to a buffer.
var writer = default(NetworkWriter);

// 1) You can manually manage the buffer:
/* 1) */ byte[] buffer = memoryPool.Rent(size);
/* 1) */ writer.Initialize(ref buffer);
// NOTE: in this way the writer will NOT expand the buffer if a Write call requires more space.

// OR

// 2) You can let the writer to manage internally the buffer:
/* 2) */ writer.Initialize(memoryPool, initialSize);
// NOTE: this will automatically expand the buffer if a Write call requires more space.

// OR

// 3) You can pass a buffer to copy its initial state, but let the writer to manage its own buffer internally:
/* 3) */ byte[] initialState = memoryPool.Rent(size);
/* 3) */ writer.Initialize(memoryPool, ref initialState);
// NOTE: this will automatically expand the buffer if a Write call requires more space.

writer.Write(12);
writer.Write(24f);
writer.Write("36");
// etc
```

## Credits

- [Emanuele Manzione](https://github.com/manhunterita) for the time he spent on this library _(Hi, it's me!)_
- [Fredrik Holmström](https://github.com/fholm) for his knowledge and his great [NetCode Talk](https://www.twitch.tv/fholm)
- [Stanislav Denisov](https://github.com/nxrighthere) for his great [NanoSockets](https://github.com/nxrighthere/NanoSockets) library
