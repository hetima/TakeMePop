//
//  TPFileItem.m
//  TakeMePop

#import "TPFileItem.h"

@implementation TPFileItem

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

@end
