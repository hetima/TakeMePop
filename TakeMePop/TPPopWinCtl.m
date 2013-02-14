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
    [[NSNotificationCenter defaultCenter]removeObserver:self];
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
    [self.oTableView setupDrag];
    [[NSNotificationCenter defaultCenter]addObserver:self
                                            selector:@selector(noteFileDropped:)
                                            name:TPPopWinTableViewFileDroppedNotification
                                            object:self.oTableView];
    
    
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

- (void)noteFileDropped:(NSNotification*)note
{
    NSArray* files=[[note userInfo]objectForKey:@"files"];
    
    NSMutableArray *arrayKVC=[self mutableArrayValueForKey:@"items"];
    for (NSString* urlStr in files) {
        TPFileItem* itm=[[TPFileItem alloc]initWithFileURLString:urlStr];
        if (itm) {
            [arrayKVC addObject:itm];
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


#pragma mark - drag drop

/*
- (void)drawRect:(NSRect)dirtyRect
{
    if (self.dragEnter) {
    }else {
    }
    [super drawRect:dirtyRect];
}
*/

- (void)setupDrag
{
    self.dragEnter=NO;
    NSArray* types=@[NSFilenamesPboardType];
    [self registerForDraggedTypes:types];
}


- (NSDragOperation)draggingEntered:(id <NSDraggingInfo>)sender {
    if ([sender draggingSource]==self) {
        return NSDragOperationNone;
    }
    
    // hope dragging items are valid
    BOOL canHandle=YES;
    self.dragEnter=canHandle;
    //currently no visual feedback
    //[self setNeedsDisplay:YES];
    
    //change cursor
    return NSDragOperationCopy;
}

- (NSDragOperation)draggingUpdated:(id < NSDraggingInfo >)sender
{
    if (self.dragEnter) {
        return NSDragOperationCopy;
    }
    return NSDragOperationGeneric;

}

- (void)draggingExited:(id < NSDraggingInfo >)sender
{
    if ([sender draggingSource]==self) {
        return;
    }
    self.dragEnter=NO;
    //currently no visual feedback
    //[self setNeedsDisplay:YES];
    
}

- (void)draggingEnded:(id < NSDraggingInfo >)sender
{
    if ([sender draggingSource]==self) {
        return;
    }
    self.dragEnter=NO;
    [self setNeedsDisplay:YES];
    
}


- (BOOL)performDragOperation:(id <NSDraggingInfo>)sender
{
    if ([sender draggingSource]==self) {
        return NO;
    }
    NSPasteboard* pb=[sender draggingPasteboard];
    NSArray* pasteboardItems=[pb pasteboardItems];
    NSMutableArray* files=[[NSMutableArray alloc]initWithCapacity:[pasteboardItems count]];
    
    for (NSPasteboardItem* pitm in pasteboardItems) {
        NSString* file=[pitm stringForType:@"public.file-url"];
        [files addObject:file];
    }
    
    [[NSNotificationCenter defaultCenter]postNotificationName:TPPopWinTableViewFileDroppedNotification
                                                       object:self userInfo:@{@"files":files}];
    
    return YES;
}

@end
