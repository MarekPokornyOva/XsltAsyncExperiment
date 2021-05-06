This branch contains attempt to make async XSLT.

### Motivation:
An XML file is XSLT usual source. However XSLT implementation might use XSLT Extension Objects providing additional data to a transformation. The data providers could be asynchronous.
Also, woudn't it make sence to make IXPathNavigable and XPathNodeIterator async?

### Development:
I tried to modify the QIL to CIL compiler to produce async/awaitable code.

### Status:
Failed :-(  
It's possible to modify the compiled methods to be async but the powerful methods are uninterruptible which effectively complicates things much.

###### Technical explanation:
```
//Sync code is fine:
ldarg_1 //the Context
ldc_i4_0 //a constant value
call ASyncMethodWhichReturnsAValue //push AValue on stack
call AMethod //call AMethod on Context consuming both the constant and AValue
```

```
//Async version can't work
ldarg_1 //the Context
ldc_i4_0 //a constant value
call AnAsyncMethodWhichReturnsAValue //push ValueTask<type-of-AValue> on stack
if (!ValueTask<type-of-AValue>.GetAwaiter().IsCompleted) //break if the prev async method haven't finished yet.
{
   ValueTask<type-of-AValue>.GetAwaiter().OnCompleted(this method);
   return;
}
//ContinuationLabel:
ValueTask<type-of-AValue>.GetAwaiter().GetResult(); //push AValue on stack
call AnAsyncMethod //call AnAsyncMethod on Context consuming both the constant and AValue
```

### Conclusion:
It would need to highly redesign the compiler to reorder the instructions. But that is far from the experiment goal.  
Maybe it could be possible to count the stack size before the OnCompleted method call and store the values to the temporary fields. And restore them after continuation.
