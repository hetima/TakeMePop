//
//  TPItem.h
//  TakeMePop


#import <Foundation/Foundation.h>

@interface TPItem : NSObject
@property (nonatomic, strong) NSString* name;
@property (nonatomic, strong) NSImage* icon;

- (id)pasteboardItem;

@end
