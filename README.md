This branch contains attempt to make async XSLT.

### Motivation:
An XML file is XSLT usual source. However XSLT implementation might use XSLT Extension Objects providing additional data to a transformation. The data providers could be asynchronous.
Also, woudn't it make sence to make IXPathNavigable and XPathNodeIterator async?

### Development:
I tried to modify the QIL to CIL compiler to produce async/awaitable code.

### Status:
Success :-)  
The code uses async/await both internally and during extension object methods invocation.

### Code changes explanation:
The code changes is split to commits to simplify understanding those.
1. ##### Support already completed and/or cancelled ValueTask internally.  
   Initial change modifying XslCompiledTransform's method to be async. However, all awaiting still happens sync.
2. ##### Use fields instead of locals.  
   Save all temporary values in fields instead of locals. That allows to break the method flow. The step is preparation for the real async.
3. ##### Support async/await internally.  
   Make all internals async and await asynchronously (= break the method flow and free its thread).
4. ##### Support async/await for extension objects' methods.
   Allow to invoke async extension objects' methods.

### Conclusion:
Original MS' QIL to CIL compiler uses stack heavily in executive methods. Unfortunatelly, that troubles usage in async. Some methods were modified to became async but others remaines and might cause invalid IL code when used; especially XSLT functions.  
Also enumeration over IXPathNavigable and XPathNodeIterator stays sync.