//
//  TPAppDelegate.m
//  TakeMePop


#import "TPAppDelegate.h"
#import "TPPopWinCtl.h"
#import "TPFinderConnect.h"
#import "TPFileItem.h"

@implementation TPAppDelegate



- (void)application:(NSApplication *)sender openFiles:(NSArray *)filenames
{
    [self createPopWindowWithFiles:filenames];
    [sender replyToOpenOrPrint:NSApplicationDelegateReplySuccess];
    
}

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
    [self createPopWindowWithFinderSelection];
    if ([self.popWinCtls count]==0) {
        [NSApp terminate:self];
    }
    return NO;
}

- (void)applicationWillFinishLaunching:(NSNotification *)notification
{
    self.popWinCtls=[@[] mutableCopy];
}

- (void)applicationDidFinishLaunching:(NSNotification *)aNotification
{
    if ([self.popWinCtls count]==0) {
        [self createPopWindowWithFinderSelection];
        if ([self.popWinCtls count]==0) {
            [NSApp terminate:self];
        }
    }
}

- (void)createPopWindowWithItems:(NSMutableArray*)items parentItem:(TPFileItem*)parentItem
{
    if (!items) {
        return;
    }
    TPPopWinCtl *winCtl = [[TPPopWinCtl alloc]initWithWindowNibName:@"TPPopWinCtl"];
    
    winCtl.parentItem=parentItem;
    winCtl.items=items;
    winCtl.includesParent=[[NSUserDefaults standardUserDefaults]boolForKey:pkIncludesTarget];
    
    [winCtl showWindow:nil];
    
    //observe close
    [[NSNotificationCenter defaultCenter]addObserver:self selector:@selector(popWindowWillClose:) name:NSWindowWillCloseNotification object:[winCtl window]];
    [self.popWinCtls addObject:winCtl];
    
}

- (void)createPopWindowWithFinderWindows
{
    NSMutableArray* items=nil;
    TPFinderConnect* fc=[[TPFinderConnect alloc]init];
    NSArray* files=[fc finderWindowTargets];
    
    if ([files count]) {
        items=[[NSMutableArray alloc]initWithCapacity:[files count]];
        
        for (NSString* urlStr in files) {
            TPFileItem* itm=[[TPFileItem alloc]initWithFileURLString:urlStr];
            if (itm) {
                [items addObject:itm];
            }
        }
    }else{
        items=[[NSMutableArray alloc]initWithObjects:[TPFileItem desktopFolderItem], nil];
    }
    
    //create
    [self createPopWindowWithItems:items parentItem:nil];
}

- (void)createPopWindowWithFinderSelection
{
    TPFinderConnect* fc=[[TPFinderConnect alloc]init];
    NSArray* files=[fc selectedFiles];
    NSString* targetFile=nil;
    TPFileItem* parentItem=nil;

    targetFile=[fc selectedTarget];
    
    if (![files count]) {
        BOOL listFinderWindows=[[NSUserDefaults standardUserDefaults]boolForKey:pkListFinderWindowsIfNoSelection];
        if (listFinderWindows) {
            [self createPopWindowWithFinderWindows];
            return;
        }else{
            if (targetFile) {
                files=@[targetFile];
                targetFile=nil;
            }else{
                return;
            }
        }
    }

    
    /*
    
    if (![files count] && targetFile) {
        files=@[targetFile];
        targetFile=nil;
    }*/
    
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
        [self createPopWindowWithItems:items parentItem:parentItem];
    }
    
}

- (void)createPopWindowWithFiles:(NSArray*)files
{
    NSMutableArray* items=[[NSMutableArray alloc]initWithCapacity:[files count]];
    for (NSString* path in files) {
        TPFileItem* itm=[[TPFileItem alloc]initWithFilePath:path];
        if (itm) {
            [items addObject:itm];
        }
    }
    
    [self createPopWindowWithItems:items parentItem:nil];
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
