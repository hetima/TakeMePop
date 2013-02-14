//
//  TPPopWinCtl.h
//  TakeMePop
//


#import <Cocoa/Cocoa.h>

#define TPPopWinTableViewFileDroppedNotification @"TPPopWinTableViewFileDroppedNotification"

@class TPFileItem, TPPopWinTableView;

@interface TPPopWinCtl : NSWindowController
@property (nonatomic,strong) TPFileItem* parentItem;
@property (nonatomic,strong) NSMutableArray* items;
@property (assign) IBOutlet TPPopWinTableView  *oTableView;
@property (assign) IBOutlet NSMenuItem  *oVersionInfoMenuItem;
@property BOOL includesParent;

- (IBAction)actOpenWebSite:(id)sender;

@end


@interface TPPopWinTableView : NSTableView
@property BOOL dragEnter;

- (void)setupDrag;
- (id)clickedIndexSetIncludesSelection;

@end
