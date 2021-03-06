﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Models;

namespace Microsoft.Sarif.Viewer.VisualStudio
{
    internal static class CodeFlowToTreeConverter
    {
        internal static List<AnalysisStepNode> Convert(CodeFlow codeFlow, Run run, int resultId, int runIndex)
        {
            var root = new AnalysisStepNode(resultId: resultId, runIndex: runIndex)
            {
                Children = new List<AnalysisStepNode>(),
            };

            ThreadFlow threadFlow = codeFlow.ThreadFlows?[0];

            if (threadFlow != null)
            {
                int lastNestingLevel = 0;
                AnalysisStepNode lastParent = root;
                AnalysisStepNode lastNewNode = null;

                foreach (ThreadFlowLocation location in threadFlow.Locations)
                {
                    ArtifactLocation artifactLocation = location.Location?.PhysicalLocation?.ArtifactLocation;

                    if (artifactLocation != null)
                    {
                        Uri uri = location.Location?.PhysicalLocation?.ArtifactLocation?.Uri;

                        if (uri == null && artifactLocation.Index > -1)
                        {
                            artifactLocation.Uri = run.Artifacts[artifactLocation.Index].Location.Uri;
                        }
                    }

                    var newNode = new AnalysisStepNode(resultId: resultId, runIndex: runIndex)
                    {
                        Location = location,
                        Children = new List<AnalysisStepNode>(),
                    };

                    if (location.NestingLevel > lastNestingLevel)
                    {
                        // The previous node was a call, so this new node's parent is that node
                        lastParent = lastNewNode;
                    }
                    else if (location.NestingLevel < lastNestingLevel)
                    {
                        // The previous node was a return, so this new node's parent is the previous node's grandparent
                        lastParent = lastNewNode.Parent.Parent;
                    }

                    newNode.Parent = lastParent;
                    lastParent.Children.Add(newNode);
                    lastNewNode = newNode;
                    lastNestingLevel = location.NestingLevel;
                }

                root.Children.ForEach(n => n.Parent = null);
            }

            return root.Children;
        }
    }
}
