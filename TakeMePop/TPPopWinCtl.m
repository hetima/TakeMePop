//
//  TPPopWinCtl.m
//  TakeMePop
//


#import "TPPopWinCtl.h"
#import "TPFileItem.h"

@interface TPPopWinCtl ()

@end

@implementation TPPopWinCtl
@synthesize includesParent=_includesParent;

- (void)dealloc
{
    [[NSUserDefaults standardUserDefaults] removeObserver:self forKeyPath:pkIncludesTarget];
    [self.oTableView setDelegate:nil];
    [self.oTableView setDataSource:nil];   
}

- (void)windowDidLoad
{
    [super windowDidLoad];
    //observe for defaultStorage
    [[NSUserDefaults standardUserDefaults] addObserver:self forKeyPath:pkIncludesTarget
                                               options:(NSKeyValueObservingOptionNew) context:NULL];
    
    //above NSModalPanelWindowLevel
    [self.window setLevel:NSMainMenuWindowLevel];
    
    [self.oTableView setDraggingSourceOperationMask:NSDragOperationEvery forLocal:NO];
    [self.oTableView setAllowsEmptySelection:YES];
    [self.oTableView.menu setFont:[NSFont systemFontOfSize:12.0]];
    
    //version information
    static NSAttributedString* versionInfoAttributedTitle=nil;
    if (!versionInfoAttributedTitle) {
        NSAttributedString* attributedTitle=[self.oVersionInfoMenuItem attributedTitle];
        NSRange range;
        NSDictionary *attrs=[attributedTitle attributesAtIndex:0 effectiveRange:&range];
        NSString* version=[[NSBundle mainBundle]objectForInfoDictionaryKey:@"CFBundleShortVersionString"];
        version=[NSString stringWithFormat:@"version %@", version];
        versionInfoAttributedTitle=[[NSAttributedString alloc]initWithString:version attributes:attrs];
    }
    [self.oVersionInfoMenuItem setAttributedTitle:versionInfoAttributedTitle];
}


- (void)observeValueForKeyPath:(NSString *)keyPath ofObject:(id)object change:(NSDictionary *)change context:(void *)context
{
    if(object==[NSUserDefaults standardUserDefaults]){
        if ([keyPath isEqual:pkIncludesTarget]) {
            BOOL newValue=[[change objectForKey:NSKeyValueChangeNewKey]boolValue];
            self.includesParent=newValue;
        }
    }
}


- (BOOL)includesParent
{
    return _includesParent;
}

- (void)setIncludesParent:(BOOL)includesParent
{
    if (_includesParent==includesParent || !self.parentItem)return;
    
    _includesParent=includesParent;
    
    NSMutableArray *arrayKVC=[self mutableArrayValueForKey:@"items"];
    if (includesParent) {
        [arrayKVC insertObject:self.parentItem atIndex:0];
    } else  {
        [arrayKVC removeObjectIdenticalTo:self.parentItem];
    }
}

- (IBAction)actOpenWebSite:(id)sender
{
    NSURL* URL=[NSURL URLWithString:kApplicationWebSiteURL];
    [[NSWorkspace sharedWorkspace]openURL:URL];
}

- (IBAction)actMenuRemoveSelection:(id)sender
{
    NSIndexSet* iset=[self.oTableView clickedIndexSetIncludesSelection];
    if (iset) {
        NSMutableArray *arrayKVC=[self mutableArrayValueForKey:@"items"];
        [arrayKVC removeObjectsAtIndexes:iset];
    }
}

- (IBAction)actMenuSwitchToFolderContents:(id)sender
{
    NSInteger row=[self.oTableView clickedRow];
    if (row>=0) {
        TPFileItem* itm=[self.items objectAtIndex:row];
        BOOL includesHiddenFiles=NO;
        if ([sender tag]==kSwitchToFolderContentsIncludesHiddenFilesTag) {
            includesHiddenFiles=YES;
        }
        
        NSMutableArray* contents=[itm directoryContentsIncludesHiddenFiles:includesHiddenFiles];
        if ([contents count]) {
            itm.icon=[NSImage imageNamed:@"NSPathTemplate"];
            self.parentItem=itm;
            if (self.includesParent) {
                [contents insertObject:itm atIndex:0];
            }
            self.items=contents;
        }
    }
}

- (BOOL)validateMenuItem:(NSMenuItem *)menuItem
{
    if ([menuItem action] == @selector(actMenuRemoveSelection:) ) {
        return [self.oTableView clickedIndexSetIncludesSelection] ? YES:NO;
    }else if ([menuItem action] == @selector(actMenuSwitchToFolderContents:) ) {
        NSInteger row=[self.oTableView clickedRow];
        if (row>=0) {
            TPFileItem* itm=[self.items objectAtIndex:row];
            return [itm isDirectory];
        }
    }
    
    return  NO;
}

#pragma mark - TableView

- (id < NSPasteboardWriting >)tableView:(NSTableView *)tableView pasteboardWriterForRow:(NSInteger)row
{
    TPItem* itm=[self.items objectAtIndex:row];
    return [itm pasteboardItem];
}

- (NSDragOperation)tableView:(NSTableView*)tableView validateDrop:(id <NSDraggingInfo>)info proposedRow:(NSInteger)row proposedDropOperation:(NSTableViewDropOperation)operation;
{
    return NSDragOperationNone;
}

- (BOOL)tableView:(NSTableView*)tableView acceptDrop:(id <NSDraggingInfo>)info row:(int)row dropOperation:(NSTableViewDropOperation)operation
{
    return NO;
}

- (void)tableView:(NSTableView *)tableView draggingSession:(NSDraggingSession *)session endedAtPoint:(NSPoint)screenPoint operation:(NSDragOperation)operation
{
    if (operation!=NSDragOperationNone) {
        if ([[NSUserDefaults standardUserDefaults]boolForKey:pkCloseAfterDone]) {
            [self.window performSelector:@selector(performClose:) withObject:nil afterDelay:0.5];
        }
    }
}


@end




@implementation TPPopWinTableView

- (NSIndexSet*)clickedIndexSetIncludesSelection
{
    NSInteger row=[self clickedRow];
    if (row>=0) {
        NSIndexSet* selection=[self selectedRowIndexes];
        if([selection containsIndex:row]){
            return selection;
        }else{
            return [NSIndexSet indexSetWithIndex:row];
        }
    }
    return nil;
}


@end
