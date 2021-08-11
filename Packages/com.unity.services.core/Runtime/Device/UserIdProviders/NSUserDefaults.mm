#include <string>

#ifdef __cplusplus
extern "C" {
#endif

char* UOCPUserDefaultsGetString(const char *key) {
    NSString* stringKey = [NSString stringWithUTF8String:key];
    NSString* stringValue = [[NSUserDefaults standardUserDefaults] stringForKey:stringKey];
    if (!stringValue) {
        return nil;
    }
    return strdup([stringValue UTF8String]);
}

void UOCPUserDefaultsSetString(const char *key, const char *value) {
    NSString* stringKey = [NSString stringWithUTF8String:key];
    NSString* stringValue = [NSString stringWithUTF8String:value];
    [[NSUserDefaults standardUserDefaults] setValue:stringValue forKey:stringKey];
    [[NSUserDefaults standardUserDefaults] synchronize];
}

#ifdef __cplusplus
}
#endif
