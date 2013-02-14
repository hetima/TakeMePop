//
//  TPFinderConnect.m
//  TakeMePop


#import "TPFinderConnect.h"



@implementation TPFinderConnect

- (id)init
{
    self = [super init];
    if (self) {
        self.finderApp=[SBApplication applicationWithBundleIdentifier:@"com.apple.finder"];
    }
    return self;
}

- (NSMutableArray*)selectedFiles
{
    NSArray* ary=[[self.finderApp selection]get];
    if ([ary count]==0) {
        return nil;
    }
    
    NSMutableArray* result=[[NSMutableArray alloc]initWithCapacity:[ary count]];
    for (FinderItem* itm in ary) {
        NSString* urlString=[self urlStringOfFinderItem:itm];
        if (urlString) {
            [result addObject:urlString];
        }
    }
    return [result count] ? result:nil;
}

- (NSMutableArray*)finderWindowTargets
{
    NSArray* finderWindows=[[self.finderApp FinderWindows]get];
    if ([finderWindows count]<1) {
        return nil;
    }
    NSMutableArray* targets=[NSMutableArray arrayWithCapacity:[finderWindows count]];
    for (FinderFinderWindow* finderWin in finderWindows) { //FinderFinderWindow
        if ([finderWin collapsed]) {
            continue;
        }
        
        NSString* target=[self targetOfFinderWindow:finderWin];
        if (target && ![targets containsObject:target]) {
            [targets addObject:target];
        }
    }
    return targets;
}


- (NSString*)selectedTarget
{
    FinderFinderWindow* finderWin=[self frontmostFinderWindow];
    return [self targetOfFinderWindow:finderWin];
}

- (FinderFinderWindow*)frontmostFinderWindow
{
    SBElementArray* ary=[self.finderApp FinderWindows];
    if ([ary count]<1) {
        return nil;
    }
    FinderFinderWindow* finderWin=[[ary objectAtIndex:0]get];
    return finderWin;
}

// file url as NSString
- (NSString*)urlStringOfFinderItem:(FinderItem*)item
{
    //NSString* urlStr=item.URL; //almost works fine, but
    // startupDisk of application "Finder" is not FinderItem but SBObject does not have @property URL.
    NSString* urlStr = [[item propertyWithCode:'pURL']get];

    if (![urlStr respondsToSelector:@selector(hasPrefix:)] || ![urlStr hasPrefix:@"file:"]) {
        return nil;
    }
    return urlStr;
}

- (NSString*)targetOfFinderWindow:(FinderFinderWindow*)finderWin
{
    return [self urlStringOfFinderItem:[finderWin.target get]];
}


@end
