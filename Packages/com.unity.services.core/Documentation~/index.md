# Services Core
This package provides a solution to initialize all game services in a single call
and defines common components used by multiple game service packages.
These components are standardized and aim to unify the overall experience of working with game service packages.

## Installation
To add the Services Core package to your project, please use the Package Manager UI to install the latest version of the package.

## Package contents

### Initialize game services
To initialize all game services at once you just have to call `UnityServices.Initialize()`.
It returns an `IAsyncOperation` object to enable you to monitor the initialization's progression.

#### Using a coroutine
```cs
IEnumerator InitializeServicesRoutine()
{
    IAsyncOperation servicesInitialization = UnityServices.Initialize();
    while (!servicesInitialization.IsDone)
    {
        yield return null;
    }

    if (servicesInitialization.Status == AsyncOperationStatus.Succeeded)
    {
        // All services are now initialized.
    }
    else
    {
        // An error occured during services initialization.
    }
}
```

#### Using a callback
```cs
void InitializeServices()
{
    UnityServices.Initialize().Completed += servicesInitialization =>
    {
        if (servicesInitialization.Status == AsyncOperationStatus.Succeeded)
        {
            // All services are now initialized.
        }
        else
        {
            // An error occured during services initialization.
        }
    };
}
```

## Technical details

### Requirements
* Supported Unity Editor: 2019.4 and later.
