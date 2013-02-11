//
//  TPFileItem.m
//  TakeMePop

#import "TPFileItem.h"

@implementation TPFileItem

- (id)initWithFileURLString:(NSString*)urlStr
{
    self = [super init];
    if (self) {
        self.urlStr=urlStr;
        self.name=[urlStr lastPathComponent];
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
