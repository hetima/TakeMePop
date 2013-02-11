//
//  TPAppDelegate.m
//  TakeMePop


#import "TPAppDelegate.h"
#import "TPPopWinCtl.h"
#import "TPFinderConnect.h"
#import "TPFileItem.h"

@implementation TPAppDelegate

- (BOOL)applicationShouldTerminateAfterLastWindowClosed:(NSApplication *)sender
{
    if ([self.popWinCtls count]) {
        //TPPopWin still exists.
        return NO;
    }
    return YES;
}

- (BOOL)applicationShouldHandleReopen:(NSApplication *)sender hasVisibleWindows:(BOOL)flag
{
    [self doNewPopWindow];
    return NO;
}

- (void)applicationDidFinishLaunching:(NSNotification *)aNotification
{
    self.popWinCtls=[@[] mutableCopy];
    
    [self doNewPopWindow];
}


- (void)doNewPopWindow
{
    TPFinderConnect* fc=[[TPFinderConnect alloc]init];
    NSArray* files=[fc selectedFiles];
    NSString* targetFile=nil;
    TPFileItem* parentItem=nil;
    
    targetFile=[fc selectedTarget];
    
    if (![files count] && targetFile) {
        files=@[targetFile];
        targetFile=nil;
    }
    
    if (targetFile) {
        parentItem=[[TPFileItem alloc]initWithFileURLString:targetFile];
        parentItem.icon=[NSImage imageNamed:@"NSPathTemplate"];
    }

    if ([files count]) {
        NSMutableArray* items=[[NSMutableArray alloc]initWithCapacity:[files count]+1];
        
        for (NSString* urlStr in files) {
            TPFileItem* itm=[[TPFileItem alloc]initWithFileURLString:urlStr];
            if (itm) {
                [items addObject:itm];
            }
        }
        
        //create
        TPPopWinCtl *winCtl = [[TPPopWinCtl alloc]initWithWindowNibName:@"TPPopWinCtl"];

        winCtl.parentItem=parentItem;
        winCtl.items=items;
        winCtl.includesParent=[[NSUserDefaults standardUserDefaults]boolForKey:pkIncludesTarget];
        
        [winCtl showWindow:nil];
        
        //observe close
        [[NSNotificationCenter defaultCenter]addObserver:self selector:@selector(popWindowWillClose:) name:NSWindowWillCloseNotification object:[winCtl window]];
        [self.popWinCtls addObject:winCtl];
    }
    
    if ([self.popWinCtls count]==0) {
        [NSApp terminate:self];
    }
}

- (void)popWindowWillClose:(NSNotification *)aNotification
{
    NSMutableArray* ctls=self.popWinCtls;

    for (TPPopWinCtl *winCtl in ctls) {
        if (winCtl.window==[aNotification object]) {
            //remove from list
            [[NSNotificationCenter defaultCenter]removeObserver:self name:NSWindowWillCloseNotification object:[aNotification object]];
            [self.popWinCtls removeObjectIdenticalTo:winCtl];
            break;
        }
    }
}

@end
