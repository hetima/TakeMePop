//
//  TPFileItem.h
//  TakeMePop


#import "TPItem.h"

@interface TPFileItem : TPItem
@property (nonatomic, strong)NSString* urlStr;

- (id)initWithFilePath:(NSString*)path;
- (id)initWithFileURLString:(NSString*)urlStr;

@end
