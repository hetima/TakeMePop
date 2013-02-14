//
//  TPFileItem.m
//  TakeMePop

#import "TPFileItem.h"

@implementation TPFileItem

+ (TPFileItem*)desktopFolderItem
{
    TPFileItem* itm=[[TPFileItem alloc]initWithFilePath:[@"~/Desktop" stringByStandardizingPath]];
    return itm;
}

- (id)initWithFilePath:(NSString*)path
{
    self = [super init];
    if (self) {
        NSURL* URL=[NSURL fileURLWithPath:path];
        self.urlStr=[URL absoluteString];
        
        NSImage* icon=[[NSWorkspace sharedWorkspace]iconForFile:path];
        self.icon=icon;
        self.name=[path lastPathComponent];
        
    }
    return self;
}

- (id)initWithFileURLString:(NSString*)urlStr
{
    self = [super init];
    if (self) {
        self.urlStr=urlStr;
        NSURL* URL=[NSURL URLWithString:urlStr];

        NSImage* icon=[[NSWorkspace sharedWorkspace]iconForFile:URL.path];
        self.icon=icon;
        self.name=[URL.path lastPathComponent];

    }
    return self;
}

- (id)pasteboardItem
{
    NSPasteboardItem* pitm=[[NSPasteboardItem alloc]init];
    [pitm setString:self.urlStr forType:@"public.file-url"];
    
    return pitm;
}

- (BOOL)isDirectory
{
    BOOL isDirectory;
    [[NSFileManager defaultManager]fileExistsAtPath:[[NSURL URLWithString:self.urlStr]path] isDirectory:&isDirectory];
    
    return isDirectory;
}

- (NSMutableArray*)directoryContentsIncludesHiddenFiles:(BOOL)includesHiddenFiles
{
    NSDirectoryEnumerationOptions mask=NSDirectoryEnumerationSkipsHiddenFiles;
    if (includesHiddenFiles) {
        mask=0;
    }
    NSArray *ary=[[NSFileManager defaultManager]contentsOfDirectoryAtURL:[NSURL URLWithString:self.urlStr] includingPropertiesForKeys:@[NSURLEffectiveIconKey] options:mask error:nil];
    
    NSMutableArray* result=[[NSMutableArray alloc]initWithCapacity:[ary count]];
    for (NSURL* url in ary) {
        NSImage* img;
        [url getResourceValue:&img forKey:NSURLEffectiveIconKey error:nil];

        
        TPFileItem* itm=[[TPFileItem alloc]init];
        itm.icon=img;
        itm.urlStr=[url absoluteString];
        itm.name=[url.path lastPathComponent];
        [result addObject:itm];
    }
    return result;
}

@end
