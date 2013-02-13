//
//  TPPopWinCtl.h
//  TakeMePop
//


#import <Cocoa/Cocoa.h>

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

- (id)clickedIndexSetIncludesSelection;

@end
