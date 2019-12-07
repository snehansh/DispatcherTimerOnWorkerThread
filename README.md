# DispatcherTimerOnWorkerThread
The repo is an extension to the existing code sample created and shared by Josh Smith on CodeProject.com

In the original code sample, the execution logic was in a method(ExecuteTestAsync) which is dervied from abstract method in the base class.
This could be an issue because one Test class will be needed for each test case you want to test. 
Typically, one test is in one test method and each test method is isolated from the other test methods.
I have modified the original code sample by passing the logic in method(ExecuteTestAsync) as an action from `Test` method. The action is then added as a parameter to the thread. The action is then passed to `Dispatcher.BeginInvoke` call. 
In this way, the base class(TestWithActiveDispatcher) do not need to know what `Action` is and the `Test` methods are isolated.
