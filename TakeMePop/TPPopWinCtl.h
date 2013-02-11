//
//  TPPopWinCtl.h
//  TakeMePop
//


#import <Cocoa/Cocoa.h>

@class TPFileItem;

@interface TPPopWinCtl : NSWindowController
@property (nonatomic,strong) TPFileItem* parentItem;
@property (nonatomic,strong) NSMutableArray* items;
@property (assign) IBOutlet NSTableView  *oTableView;
@property (assign) IBOutlet NSMenuItem  *oVersionInfoMenuItem;
@property BOOL includesParent;

- (IBAction)actOpenWebSite:(id)sender;

@end

