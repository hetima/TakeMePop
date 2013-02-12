//
//  TPFileItem.h
//  TakeMePop


#import "TPItem.h"

@interface TPFileItem : TPItem
@property (nonatomic, strong)NSString* urlStr;

+ (TPFileItem*)desktopFolderItem;
- (id)initWithFilePath:(NSString*)path;
- (id)initWithFileURLString:(NSString*)urlStr;

@end
