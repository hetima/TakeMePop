//
//  TPAppDelegate.h
//  TakeMePop


#import <Cocoa/Cocoa.h>

@interface TPAppDelegate : NSObject <NSApplicationDelegate>
@property (nonatomic, strong) NSMutableArray *popWinCtls;

@property (assign) IBOutlet NSWindow *window;

@end
