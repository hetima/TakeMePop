//
//  TPFinderConnect.h
//  TakeMePop


#import <Foundation/Foundation.h>
#import "Finder.h"

@interface TPFinderConnect : NSObject
@property(strong)FinderApplication* finderApp;


- (NSString*)selectedTarget;
- (NSMutableArray*)selectedFiles;

@end
